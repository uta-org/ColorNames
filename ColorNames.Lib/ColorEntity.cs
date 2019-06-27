namespace ColorNames.Lib
{
    public class ColorEntity
    {
        public string Name { get; set; }

        public string Hex { get; set; }

        public override string ToString()
        {
            return $"{{Name={Name}, Hex={Hex}}}";
        }
    }
}