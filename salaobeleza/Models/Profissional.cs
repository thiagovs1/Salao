namespace SalaoBeleza.Models
{
    public class Profissional
    {
        public int Id { get; set; }

        public string Nome { get; set; } = "";

        public string Profissao { get; set; } = "";

        public string Foto { get; set; } = "";

        public bool Ativo { get; set; } = true;
    }
}