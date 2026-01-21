using System.Collections.Generic;

namespace SystemLogin.Core;

public sealed record SortingOrder(
    int OrderId,
    string CustomerName,
    IReadOnlyList<SortingOrderItem> Items
);

public sealed record SortingOrderItem(
    int ProductId,
    string ProductName,
    BlockColor Color,
    int Quantity
);
