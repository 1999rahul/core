namespace UnitOfWorks.Helper
{
    public class TableNameAttribute: Attribute
    {
        public string Name { get; }

        public TableNameAttribute(string Name)
        {
            this.Name = Name;
        }
    }
}
