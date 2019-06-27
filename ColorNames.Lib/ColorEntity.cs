using CsvHelper.Configuration.Attributes;

namespace ColorNames.Lib
{
    public class ColorEntity
    {
        // [Name("name")]
        [Index(0)]
        public string Name { get; set; }

        // [Name("hex")]
        [Index(1)]
        public string Hex { get; set; }

        public override string ToString()
        {
            return $"{{Name={Name}, Hex={Hex}}}";
        }
    }
}