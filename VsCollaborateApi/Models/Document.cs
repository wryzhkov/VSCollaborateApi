namespace VsCollaborateApi.Models
{
    public class Document
    {
        public Document(Guid id, string name, string owner)
        {
            Id = id;
            Name = name;
            Owner = owner;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
    }
}