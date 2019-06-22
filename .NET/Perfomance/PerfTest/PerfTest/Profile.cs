using System;

namespace PerfTest
{ 
    public class Profile 
    {
        public Location[] Locations { get; set; }

        public string Name { get; set; }

        public string AvatarUrl { get; set; }

        public DateTime? Birthday { get; set; }

        public string Mood { get; set; }

        public string About { get; set; }

        public string Language { get; set; }

        public string Website { get; set; }
    }
}