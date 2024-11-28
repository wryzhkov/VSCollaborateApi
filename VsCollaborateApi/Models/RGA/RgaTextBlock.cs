namespace VsCollaborateApi.Models.RGA
{
    public class RgaTextBlock
    {
        public string Id { get; set; }
        public string Text { get; set; }

        public bool Visible { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is RgaTextBlock block &&
                   Id == block.Id &&
                   Text == block.Text;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Text);
        }
    }
}