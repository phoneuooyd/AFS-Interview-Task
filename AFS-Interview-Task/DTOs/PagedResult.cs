using System;
using System.Collections.Generic;

namespace AFS_Interview_Task.DTOs;

public record PagedResult<T>(
    int TotalCount,
    int Page,
    int PageSize,
    IEnumerable<T> Items
);