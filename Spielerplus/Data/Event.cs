using System;
using System.Collections;
using System.Collections.Generic;

namespace Spielerplus.Data
{
    /// <summary>
    /// structure to store spielerplus event information
    /// </summary>
    public class Event
    {
        /// <summary>
        /// event id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// event type
        /// </summary>
        /// <example>training</example>
        public string EventType { get; set; }

        /// <summary>
        /// event name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// meet date and time
        /// </summary>
        public DateTime Meet { get; set; }

        /// <summary>
        /// start of the event
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// end of the event
        /// </summary>
        public DateTime End { get; set; }

        /// <summary>
        /// list of users and their participation
        /// </summary>
        public ICollection<UserParticipation> Participations = new List<UserParticipation>();
    }
}