namespace BlankProject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            Event A= new Event();
            Event B= new Event();

            A.StartDate = new DateTime(2024,01,20);
            B.StartDate = new DateTime(2024, 01, 25);
            A.EndDate = new DateTime(2024, 02, 01);
            B.EndDate = new DateTime(2024, 02, 15);
            Console.WriteLine("Event 1 Duration: " + A.GetDuration() + " days");
            Console.WriteLine("Event 2 Duration: " + B.GetDuration() + " days");
            Console.WriteLine("Events Overlap: " + A.IsOverlapping(B));
        }


        struct Event
        {
            public DateTime StartDate;
            public DateTime EndDate;

            public double GetDuration()
            {
                TimeSpan timeSpan= EndDate - StartDate;
                
                return timeSpan.Days;
            }

            public bool IsOverlapping(Event e)
            {
                return StartDate < e.EndDate && e.StartDate < EndDate;
            }
        }
    }
}