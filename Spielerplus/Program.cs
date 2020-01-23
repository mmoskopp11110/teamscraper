using Spielerplus.Data;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Spielerplus
{
    class Program
    {
        /// <summary>
        /// automate spielerplus
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            ScrapeSpielerPlus();
        }

        #region app

        /// <summary>
        /// basic scraping console app
        /// </summary>
        static void ScrapeSpielerPlus()
        {
            Console.WriteLine("=============Spielerplus Automatisierung=============");
            Console.WriteLine("Logge dich zunächst ein.");

            // create scraper and login
            SpielerPlusScraper scraper = new SpielerPlusScraper();
            DoScraperLogin(scraper);
            
            // fetch all events
            bool gotEvents = false;
            do
            {
                
                try
                {
                    Console.WriteLine("Lade alle kommenden Termine. Bitte warten...");
                    scraper.GetAllEvents();
                    Console.WriteLine("Termine wurden geladen.");
                    gotEvents = true;
                }
                catch (NotLoggedInException ex)
                {
                    // something went wrong with the login, try again
                    Console.Error.WriteLine(ex.Message);
                    DoScraperLogin(scraper);
                }
            } while (!gotEvents);

            // main loop
            while (true)
            {
                Console.Write("Was möchtest du jetzt tun?\n" +
                    "j: Alle Termine zusagen\n" +
                    "p: Alle eigenen Antworten anzeigen\n" +
                    "r: Termine aktualisieren\n" +
                    "exit: Programm beenden\n" +
                    "Deine Wahl: ");
                string msg = Console.ReadLine();

                if (msg.ToLower().StartsWith("exit"))
                {
                    Console.WriteLine("\nProgramm wird beendet...");
                    break;
                }

                if (msg.ToLower().StartsWith("j"))
                {
                    Console.WriteLine("\nSage zu:");
                    Console.WriteLine($"{"Datum",-20} Terminname");
                    foreach (var ev in scraper.Events)
                    {
                        scraper.JoinEvent(ev);
                        Console.WriteLine($"{ev.Start,-20:dd-MM-yyyy HH:mm} {ev.Name}");
                    }
                    Console.Write("\n\n");
                }
                else if (msg.ToLower().StartsWith("p"))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"\nAntworten: \n{"Datum",-20} {"Terminname",-40} Antwort\n");
                    foreach (Data.Event ev in scraper.Events.OrderBy(e => e.Start))
                    {
                        string evResponse = "Konnte nicht ermittelt werden";
                        try
                        {
                            UserParticipation userParticipation = ev.Participations.FirstOrDefault(up => up.User.Name == scraper.FullUserName);
                            switch (userParticipation.Participation)
                            {
                                case Participation.Unassigned:
                                    evResponse = "Noch nicht zu/abgesagt";
                                    break;
                                case Participation.Going:
                                    evResponse = $"Zugesagt ({userParticipation.Reason})";
                                    break;
                                case Participation.Unsafe:
                                    evResponse = $"Unsicher ({userParticipation.Reason})";
                                    break;
                                case Participation.Absent:
                                    evResponse = $"Abgesagt ({userParticipation.Reason})";
                                    break;
                                case Participation.NotNominated:
                                    evResponse = "Nicht nominiert";
                                    break;
                            }
                        }
                        catch (Exception)
                        {
                        }
                        sb.Append($"{ev.Start,-20:dd-MM-yyyy HH:mm} {ev.Name,-40} {evResponse}\n");
                    }
                    Console.Write(sb.ToString() + "\n\n");
                }
                else if (msg.ToLower().StartsWith("r"))
                {
                    Console.WriteLine("Lade die Termine erneut...");
                    scraper.GetAllEvents();
                    Console.WriteLine("Termine wurden aktualisiert.\n");
                }
            }
        }

        #endregion

        #region helper functions

        /// <summary>
        /// ask for password and email and do the login
        /// </summary>
        /// <param name="scraper">the current <see cref="SpielerPlusScraper"/> instance</param>
        static void DoScraperLogin(SpielerPlusScraper scraper)
        {
            do
            {
                Console.Write("E-Mail Adresse: ");
                string user = Console.ReadLine().Trim();
                Console.Write("Passwort: ");
                string pass = GetPassword(); // masked pw input
                Console.Write("\n...");
                
                if (scraper.Login(user, pass))
                {
                    Console.WriteLine("Login erfolgreich!");
                }
                else
                {
                    Console.WriteLine("Login fehlgeschlagen, versuche es erneut.");
                }
            } while (!scraper.IsLoggedIn); // try until logged in
        }

        /// <summary>
        /// taken from https://stackoverflow.com/questions/3404421/password-masking-console-application#3404522
        /// </summary>
        /// <returns>the password</returns>
        static string GetPassword()
        {
            string pass = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if(key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }
            } while (true);

            return pass;
        }

        #endregion

    }
}