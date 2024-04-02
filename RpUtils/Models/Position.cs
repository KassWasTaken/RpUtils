namespace RpUtils.Models
{
    internal class Position
    {
        public float X { get; set; }
        public float Z { get; set; }

        public Position(float x, float z)
        {
            this.X = x; this.Z = z;
        }
    }
}
