namespace Domain.Common
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        // STEP 14 : Soft delete fields
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
