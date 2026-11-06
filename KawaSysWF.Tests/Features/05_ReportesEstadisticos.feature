# language: es
@reportes
Característica: Reportes estadísticos de facturación y cobranzas
  Como responsable de Finanzas
  Quiero ver gráficos con lo facturado, lo cobrado y lo que se debe
  Para tomar decisiones sobre la cobranza sin armar planillas a mano

  Antecedentes:
    Dado que el sistema tiene los datos iniciales de KawaSys
    Y que se facturaron las ventas históricas

  @smoke
  Escenario: Facturado contra cobrado por mes
    Cuando consulto el reporte de facturado contra cobrado de 2025
    Entonces el reporte mensual muestra:
      | mes | facturado | cobrado |
      | 5   | 3845000   | 2880000 |
      | 6   | 3060000   | 1860000 |
    Y los demás meses figuran en cero

  Escenario: Cobros por medio de pago
    Cuando consulto el reporte de cobros por medio de pago de 2025
    Entonces el reporte muestra, de mayor a menor:
      | etiqueta      | valor   |
      | Transferencia | 2650000 |
      | Efectivo      | 1115000 |
      | Crédito       | 685000  |
      | Cheque        | 290000  |

  Escenario: Facturas por estado
    Cuando consulto el reporte de facturas por estado de 2025
    Entonces el reporte muestra estas cantidades:
      | etiqueta | cantidad |
      | cobrada  | 11       |
      | emitida  | 4        |

  Escenario: Clientes con mayor deuda
    Cuando consulto el reporte de deuda por cliente de 2025
    Entonces el reporte muestra, de mayor a menor:
      | etiqueta                 | valor   |
      | Distribuidora Del Sur SA | 1145000 |
      | Moto Center SRL          | 1020000 |

  Escenario: Un cobro parcial se refleja en los reportes
    Dado la factura de la venta histórica número 15
    Cuando registro un cobro de 145000 con "Cheque"
    Y consulto el reporte de deuda por cliente de 2025
    Entonces el reporte muestra, de mayor a menor:
      | etiqueta                 | valor   |
      | Moto Center SRL          | 1020000 |
      | Distribuidora Del Sur SA | 1000000 |
    Y al consultar el reporte de facturas por estado de 2025 hay 1 factura en estado "parcial"

  Escenario: Facturado por sucursal
    Cuando consulto el reporte de facturado por sucursal de 2025
    Entonces el reporte muestra, de mayor a menor:
      | etiqueta       | valor   |
      | Sucursal Norte | 3900000 |
      | Sucursal Sur   | 3005000 |

  @smoke
  Escenario: Exportar el reporte como imagen
    Dado la factura de la venta histórica número 15
    Cuando registro un cobro de 145000 con "Cheque"
    Y exporto el reporte de 2025 como imagen "reporte_2025.png"
    Entonces se genera una imagen PNG de 1200 píxeles de ancho
