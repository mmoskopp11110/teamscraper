using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using HtmlAgilityPack;
using Spielerplus.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Spielerplus
{
    /// <summary>
    /// Scrape the spielerplus.de website
    /// </summary>
    public class SpielerPlusScraper
    {
        #region private fields

        /// <summary>
        /// the scraper's user
        /// </summary>
        private string _user;
        
        /// <summary>
        /// the scraper's password
        /// </summary>
        private string _password;
        
        /// <summary>
        /// the browser session
        /// </summary>
        private readonly BrowserSession _session = new BrowserSession();

        #endregion
        
        #region properties

        /// <summary>
        /// is a user logged in?
        /// </summary>
        public bool IsLoggedIn { get; private set; } = false;

        /// <summary>
        /// get the current user
        /// </summary>
        public string User => _user;

        /// <summary>
        /// The full name of the logged user
        /// </summary>
        public string FullUserName { get; private set; }
        
        /// <summary>
        /// all discovered events
        /// </summary>
        public ICollection<Event> Events = new List<Event>();
        
        #endregion

        #region public methods

        /// <summary>
        /// login a user
        /// </summary>
        /// <param name="user">email</param>
        /// <param name="pass">password</param>
        /// <returns>true if login was successful</returns>
        public bool Login(string user, string pass)
        {
            // store login info
            _user = user;
            _password = pass;
            
            // set login url
            string loginURL = SpielerplusURLs.Base + SpielerplusURLs.Login;
            _session.Get(loginURL); // get login page
            
            // input credentials
            _session.FormElements["LoginForm[email]"] = _user;
            _session.FormElements["LoginForm[password]"] = _password;
            
            // submit
            _session.Post(loginURL);
            
            // successful login sets additional cookies (4 are present by default, should go up to 7 when logged in)
            bool success = _session.Cookies.Count > 4;
            if (success) IsLoggedIn = true;
            return success;
        }

        /// <summary>
        /// get all events (no old events, only current and future ones)
        /// </summary>
        /// <exception cref="NotLoggedInException">the user is not logged in</exception>
        public void GetAllEvents()
        {
            // throw if not logged in
            if (!IsLoggedIn)
            {
                throw new NotLoggedInException("Du bist nicht eingeloggt! Bitte logge dich ein.");
            }
            
            // get initial events page
            HtmlDocument events = new HtmlDocument();
            string eventsPage = _session.Get(SpielerplusURLs.Base + SpielerplusURLs.Events).Replace("\n", "");
            events.LoadHtml(eventsPage);

            // get logged user's full name
            HtmlNodeCollection fullNameNodes = events.DocumentNode
                .SelectNodes(@"//div[" + XPathClassMatch("menu-header-sublabel") + "]");
            if (fullNameNodes == null) throw new Exception("Full username was not found on events page.");
            FullUserName = fullNameNodes[0].InnerText.Trim();

            // get events from events page
            getPageEvents(events);
            
            // initial offset is 5
            int offset = 5;
            
            // load all available future events
            while (true)
            {
                // send post request with offset to get new events
                _session.FormElements["offset"] = offset.ToString();
                _session.Post(SpielerplusURLs.Base + SpielerplusURLs.Events + @"/ajaxgetevents");
                
                // parse the response
                HtmlResponse response = JsonSerializer.Deserialize<HtmlResponse>(_session.HtmlDoc.Text);
                response.html = response.html.Replace("\n", "");
                
                // if no more events are available, count will be -1
                if (response.count < 1)
                {
                    break;
                }
                
                // load the response html into the html doc
                events.LoadHtml(response.html);
                
                // get the new events from the returned html
                getPageEvents(events);
                
                // increase offset by count
                offset += response.count;
            }
        }

        /// <summary>
        /// join an event
        /// </summary>
        /// <param name="ev"></param>
        public void JoinEvent(Event ev)
        {
            string uid = ev.Participations.FirstOrDefault(p => p.User.Name == FullUserName).User.Id;
            _session.FormElements["Participation[participation]"] = "1";
            _session.FormElements["Participation[reason]"] = "";
            _session.FormElements["Participation[type]"] = ev.EventType;
            _session.FormElements["Participation[typeid]"] = ev.Id;
            _session.FormElements["Participation[user_id]"] = uid;
            _session.Post(SpielerplusURLs.Base + SpielerplusURLs.Events + @"/ajax-participation-form");
            ev.Participations.FirstOrDefault(p => p.User.Name == FullUserName).Participation = Participation.Going;
        }

        #endregion

        #region private methods

        /// <summary>
        /// get the actual <see cref="Event"/>s from the <see cref="HtmlDocument"/>
        /// </summary>
        /// <param name="page">the <see cref="HtmlDocument"/> to extract <see cref="Event"/>s from</param>
        private void getPageEvents(HtmlDocument page)
        {
            // get event panel divs from page
            HtmlNodeCollection eventPanels = page.DocumentNode
                .SelectNodes(@"//div[" + XPathClassMatch("panel") + " and starts-with(@id,'event-')]");

            foreach (HtmlNode eventPanel in eventPanels)
            {
                string[] panelId = eventPanel.GetAttributeValue("id", "").Split('-'); // panel id
                if(panelId.Length < 3 // panel ids are "event-<eventtype>-<eventid>" strings
                   || panelId.Any(e => string.IsNullOrEmpty(e)) // substrings must not be empty
                   || Events.Any(e => e.Id == panelId[2] && e.EventType == panelId[1])) // check if this event is already present
                {
                    continue;
                }
                
                // get the name of the event
                HtmlNodeCollection titleNodes = eventPanel
                    .SelectNodes(@"./*//div[" + XPathClassMatch("panel-heading-text") + "]");
                if (titleNodes == null) continue; // no node found, skip this eventPanel //TODO report error
                HtmlNode titleNode = titleNodes[0];
                string title = "";
                if (titleNode.ChildNodes.Count(c => c.Name == "div") > 1) // do we have a subtitle?
                {
                    title = titleNode.ChildNodes.Where(c => c.Name == "div").ToArray()[0].InnerText + " - " + titleNode.ChildNodes.Where(c => c.Name == "div").ToArray()[1].InnerText;
                }
                else // no subtitle
                {
                    title = titleNode.InnerText.Trim();
                }
                
                // create the new event
                Event newEvent = new Event()
                {
                    Name = title,
                    EventType = panelId[1],
                    Id = panelId[2]
                };

                // fetch event details
                getEventDetails(newEvent, eventPanel);

                // add the new event to our event list
                Events.Add(newEvent);
            }
        }
        
        /// <summary>
        /// get details for a specific <see cref="Event"/>
        /// </summary>
        /// <param name="ev">the <see cref="Event"/> to add details to</param>
        /// <param name="eventPanel">the <see cref="HtmlNode"/> that contains the event's panel div</param>
        private void getEventDetails(Event ev, HtmlNode eventPanel)
        {
            // get event times
            HtmlNodeCollection timesNodes = eventPanel
                .SelectNodes(@"./*//div[" + XPathClassMatch("event-time-item") + "]");
            // wait for time extraction until the date is known
                
            // get participation modal for more info
            _session.FormElements["eventid"] = ev.Id;
            _session.FormElements["eventtype"] = ev.EventType;
            _session.Post(SpielerplusURLs.Base + SpielerplusURLs.Events + @"/ajaxgetparticipation");
            // parse the response
            HtmlResponse response = JsonSerializer.Deserialize<HtmlResponse>(_session.HtmlDoc.Text);
            response.html = response.html.Replace("\n", "");
            HtmlDocument participationModal = new HtmlDocument();
            participationModal.LoadHtml(response.html);
            
            // get date
            HtmlNodeCollection dateNodes = participationModal.DocumentNode
                .SelectNodes(@"//div[" + XPathClassMatch("participation-header") + "]/div[" + XPathClassMatch("subline") + "]");
            if (dateNodes != null) // date found
            {
                string date = dateNodes[0].InnerText
                    .Trim()
                    .Replace("- ", "")
                    .Replace(" Uhr", "");
                ev.Start = DateTime.ParseExact(date, "dd.MM.yy HH:mm", CultureInfo.InvariantCulture);
            }
            
            // meet and end dates
            if (timesNodes.Count == 3)
            {
                // get time string from inside the nodes
                string meet = timesNodes[0].ChildNodes.Where(c => c.Name == "div").ToArray()[1].InnerText;
                
                // if no meet time is specified, use one hour before the event's start
                if (meet == "-:-")
                {
                    ev.Meet = ev.Start.Subtract(new TimeSpan(1, 0, 0));
                }
                else
                {
                    // parse the time as a timespan
                    TimeSpan meetTime = TimeSpan.ParseExact(meet.Replace(" Uhr", ""), @"hh\:mm", CultureInfo.InvariantCulture);

                    // create datetime from the timespan
                    ev.Meet = new DateTime(ev.Start.Year, ev.Start.Month, ev.Start.Day, meetTime.Hours, meetTime.Minutes, meetTime.Seconds);
                }


                // get time string from inside the nodes
                string end = timesNodes[2].ChildNodes.Where(c => c.Name == "div").ToArray()[1].InnerText;

                // if no end time is specified, use 3 hours after the event's start
                if (end == "-:-")
                {
                    ev.End = ev.Start.AddHours(3);
                }
                else
                {
                    // parse the time as a timespan
                    TimeSpan endTime = TimeSpan.ParseExact(end.Replace(" Uhr", ""), @"HH\:mm", CultureInfo.InvariantCulture);

                    // create datetime from the timespan
                    ev.End = new DateTime(ev.Start.Year, ev.Start.Month, ev.Start.Day, endTime.Hours, endTime.Minutes, endTime.Seconds);
                }
            }
            
            // get participations
            HtmlNodeCollection pLists = participationModal.DocumentNode
                .SelectNodes(@"//div[" + XPathClassMatch("participation-list") + "]");

            // go through all lists
            for(int i=0; i<pLists.Count; i++)
            {
                // participation counter matches the participation list
                Participation participation = (Participation)i;

                // get users in this list
                HtmlNodeCollection userNodes = pLists[i]
                        .SelectNodes(@"./*//div[" + XPathClassMatch("participation-list-user") + "]");
                if(userNodes == null) continue;

                // extract user informations
                foreach (HtmlNode userNode in userNodes)
                {
                    // username, reason and user id extraction
                    string name = userNode.ChildNodes[3].ChildNodes[1].InnerText.Trim();
                    string reason = userNode.ChildNodes[3].ChildNodes[3].InnerText.Trim();
                    string uid = userNode.ChildNodes[1].Attributes[1].Value;
                    Regex re = new Regex("[0-9]+");
                    uid = re.Match(uid).Groups[0].Value;

                    // create the user with the extracted info
                    User user = new User(uid, name);

                    // create an user participation
                    UserParticipation userParticipation = new UserParticipation()
                    {
                        User = user,
                        Participation = participation,
                        Reason = reason
                    };

                    // add to the event's participation list
                    ev.Participations.Add(userParticipation);
                }
            }
        }

        /// <summary>
        /// xpath match string for partial class matches
        /// </summary>
        /// <param name="containsClass">the partial class to match</param>
        /// <returns>xpath string</returns>
        private static string XPathClassMatch(string containsClass) => @"contains(concat(' ',normalize-space(@class),' '),' " + containsClass + @" ')";

        #endregion
    }
}