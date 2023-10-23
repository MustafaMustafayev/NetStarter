﻿using SOURCE.Builders.Abstract;
using SOURCE.Models;
using SOURCE.Workers;

namespace SOURCE.Builders;

// ReSharper disable once UnusedType.Global
public class EntityConfigurationBuilder : ISourceBuilder
{
    public void BuildSourceFile(List<Entity> entities)
    {
        entities.ForEach(model =>
        {
            if (model.Configure)
            {
                SourceBuilder.Instance.AddSourceFile(Constants.EntityConfigurationPath, $"{model.Name}Configuration.cs",
                    BuildSourceText(model, null));
            }
        });
    }

    public string BuildSourceText(Entity? entity, List<Entity>? entities)
    {
        var text = """
                   using ENTITIES.Entities;
                   using Microsoft.EntityFrameworkCore;
                   using Microsoft.EntityFrameworkCore.Metadata.Builders;

                   namespace DAL.EntityFramework.Configurations;

                   public class UserConfiguration : IEntityTypeConfiguration<{entityName}>
                   {
                       public void Configure(EntityTypeBuilder<{entityName}> builder)
                       {
                           
                       }
                   }

                   """;
        text = text.Replace("{entityName}", entity!.Name);

        return text;
    }
}