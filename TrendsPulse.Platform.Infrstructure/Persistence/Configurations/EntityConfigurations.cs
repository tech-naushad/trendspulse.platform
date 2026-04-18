using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrendsPulse.Platform.Domain.Entities;

namespace TrendsPulse.Platform.Infrstructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("categories");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(c => c.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(c => c.IconCode).HasColumnName("icon_code").HasMaxLength(50);
        b.Property(c => c.ColorHex).HasColumnName("color_hex").HasMaxLength(7);
        b.Property(c => c.Type).HasColumnName("type").HasConversion<int>();
        b.Property(c => c.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
        b.Property(c => c.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
        b.Property(c => c.TenantId).HasColumnName("tenant_id");
        b.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        b.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(c => c.UpdatedAt).HasColumnName("updated_at");
        b.Property(c => c.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        b.Property(c => c.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);

        b.HasIndex(c => new { c.Name, c.TenantId }).HasDatabaseName("ix_categories_name_tenant");
        b.HasIndex(c => c.TenantId).HasDatabaseName("ix_categories_tenant_id");
        b.HasIndex(c => c.IsDeleted).HasDatabaseName("ix_categories_is_deleted");

        b.HasQueryFilter(c => !c.IsDeleted);

        b.HasMany(c => c.Items)
            .WithOne(i => i.Category)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Ignore(c => c.DomainEvents);
    }
}

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> b)
    {
        b.ToTable("items");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(i => i.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        b.Property(i => i.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
        b.Property(i => i.Description).HasColumnName("description").HasMaxLength(1000);
        b.Property(i => i.Symbol).HasColumnName("symbol").HasMaxLength(20);
        b.Property(i => i.Unit).HasColumnName("unit").HasConversion<int>();
        b.Property(i => i.CustomUnitLabel).HasColumnName("custom_unit_label").HasMaxLength(50);
        b.Property(i => i.DecimalPrecision).HasColumnName("decimal_precision").HasDefaultValue(2);
        b.Property(i => i.Status).HasColumnName("status").HasConversion<int>();
        b.Property(i => i.Visibility).HasColumnName("visibility").HasConversion<int>();
        b.Property(i => i.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
        b.Property(i => i.Tags).HasColumnName("tags").HasColumnType("jsonb");
        b.Property(i => i.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(500);
        b.Property(i => i.CategoryId).HasColumnName("category_id").IsRequired();
        b.Property(i => i.TenantId).HasColumnName("tenant_id");
        b.Property(i => i.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        b.Property(i => i.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        b.Property(i => i.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        b.Property(i => i.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);

        b.HasIndex(i => i.Slug).IsUnique().HasDatabaseName("ix_items_slug");
        b.HasIndex(i => i.CategoryId).HasDatabaseName("ix_items_category_id");
        b.HasIndex(i => i.TenantId).HasDatabaseName("ix_items_tenant_id");
        b.HasIndex(i => i.Status).HasDatabaseName("ix_items_status");
        b.HasIndex(i => new { i.TenantId, i.Status }).HasDatabaseName("ix_items_tenant_status");
        b.HasIndex(i => i.IsDeleted).HasDatabaseName("ix_items_is_deleted");

        b.HasQueryFilter(i => !i.IsDeleted);

        b.HasOne(i => i.Category).WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(i => i.DataSourceMappings).WithOne(m => m.Item)
            .HasForeignKey(m => m.ItemId).OnDelete(DeleteBehavior.Cascade);

        b.Ignore(i => i.DomainEvents);
    }
}

public class DataSourceMappingConfiguration : IEntityTypeConfiguration<DataSourceMapping>
{
    public void Configure(EntityTypeBuilder<DataSourceMapping> b)
    {
        b.ToTable("data_source_mappings");
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(m => m.ItemId).HasColumnName("item_id").IsRequired();
        b.Property(m => m.SourceType).HasColumnName("source_type").HasConversion<int>();
        b.Property(m => m.ExternalIdentifier).HasColumnName("external_identifier").HasMaxLength(300).IsRequired();
        b.Property(m => m.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
        b.Property(m => m.FetchFrequency).HasColumnName("fetch_frequency").HasConversion<int>();
        b.Property(m => m.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        b.Property(m => m.AdditionalConfig).HasColumnName("additional_config").HasColumnType("jsonb");
        b.Property(m => m.AvFunction).HasColumnName("av_function").HasMaxLength(100);
        b.Property(m => m.AvMarket).HasColumnName("av_market").HasMaxLength(20);
        b.Property(m => m.CustomEndpointUrl).HasColumnName("custom_endpoint_url").HasMaxLength(1000);
        b.Property(m => m.CustomPriceJsonPath).HasColumnName("custom_price_json_path").HasMaxLength(500);
        b.Property(m => m.CustomTimestampJsonPath).HasColumnName("custom_timestamp_json_path").HasMaxLength(500);
        b.Property(m => m.CustomHeaders).HasColumnName("custom_headers").HasColumnType("jsonb");
        b.Property(m => m.LastSuccessAt).HasColumnName("last_success_at");
        b.Property(m => m.LastAttemptAt).HasColumnName("last_attempt_at");
        b.Property(m => m.LastErrorMessage).HasColumnName("last_error_message").HasMaxLength(2000);
        b.Property(m => m.ConsecutiveFailures).HasColumnName("consecutive_failures").HasDefaultValue(0);
        b.Property(m => m.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.Property(m => m.DeletedAt).HasColumnName("deleted_at");
        b.Property(m => m.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        b.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        b.Property(m => m.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        b.Property(m => m.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);

        b.HasIndex(m => m.ItemId).HasDatabaseName("ix_dsm_item_id");
        b.HasIndex(m => new { m.ItemId, m.IsPrimary }).HasDatabaseName("ix_dsm_item_primary");
        b.HasIndex(m => new { m.SourceType, m.IsEnabled }).HasDatabaseName("ix_dsm_source_enabled");
        b.HasIndex(m => m.IsDeleted).HasDatabaseName("ix_dsm_is_deleted");

        b.HasQueryFilter(m => !m.IsDeleted);
        b.HasOne(m => m.Item).WithMany(i => i.DataSourceMappings)
            .HasForeignKey(m => m.ItemId).OnDelete(DeleteBehavior.Cascade);

        b.Ignore(m => m.DomainEvents);
    }
}
