﻿using CORE.Enums;

namespace DTO.File;

public record FileDeleteRequestDto
{
    public string HashName { get; set; }
    public EFileType Type { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
}