using System;
using System.Collections.Generic;

namespace Stores
{
    public class Store
    {
        public string StoreNo { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public Country Country { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public Manager Manager { get; set; }
        public VisitGeoPos VisitGeoPos { get; set; }
        public VisitAddress VisitAddress { get; set; }
        public List<OpeningHour> OpeningHours { get; set; }
    }

    public class Country
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class Manager
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class VisitGeoPos
    {
        public double Lon { get; set; }
        public double Lat { get; set; }
    }

    public class VisitAddress
    {
        public string StoreName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Zipcode { get; set; }
    }

    public class OpeningHour
    {
        public string Day { get; set; }
        public string OpenFrom { get; set; }
        public string OpenTo { get; set; }
    }
}
