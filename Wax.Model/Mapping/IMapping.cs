namespace tomenglertde.Wax.Model.Mapping
{
    public interface IMapping
    {
        bool IsResolved { get; }

        bool HasSelectableItems { get; }
    }
}