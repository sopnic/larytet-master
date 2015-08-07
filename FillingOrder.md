  * Call  OrdersStreamStart() to get access to the order status stream
  * Create object of type MaofOrderType
  * Call FMRLibTester.FMRUser.SendMaofOrder()
  * Check ErrNo (should be zero if ok) and reenter order if appropriate
  * Check order [order status](OrderStatus.md) by calling FMRLibTester.FMRUser.[GetOrdersMF](GetOrdersMF.md)


In the sample MaofOrder.frm there is apparently a pooling 100ms timer which checks if there is any change in the pending order status. This approach creates 100ms latency between the order closed and the system knows about that. The reasonable approach is to check the stream for possible order close - see all orders closed the at the price level similar to the pending orders and if there is something relevant call GetOrdersMF() immediately.