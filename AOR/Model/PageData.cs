namespace AOR.Model
{
    public class PageData
    {
        public uint StartTimeStamp = 0;
        public uint EndTimeStamp = 0;
        public int PageNumber = 0;
        public bool Fired = false;

        public PageData(int num, uint start, uint end)
        {
            PageNumber = num;
            StartTimeStamp = start;
            EndTimeStamp = end;
        }
    }
}