# language: es
@facturacion @KAWA-43 @KAWA-44
Característica: Generación de la factura al registrar una venta
  Como vendedor de una sucursal de Kawa
  Quiero que cada venta registrada genere su factura automáticamente
  Para no cargar dos veces la operación y que Finanzas vea lo que hay que cobrar

  Antecedentes:
    Dado que el sistema tiene los datos iniciales de KawaSys

  @smoke
  Escenario: Venta al contado genera una factura cobrada
    Cuando registro una venta a consumidor final de 2 "Honda" "XR150" a 285000 cada una con forma de pago "Efectivo"
    Entonces se genera una factura en estado "cobrada"
    Y la factura tiene un total de 570000
    Y la factura tiene un cobro de 570000 con medio de pago "Efectivo"
    Y el saldo pendiente de la factura es 0

  Esquema del escenario: Cualquier medio de pago que no sea cuenta corriente cobra la factura completa
    Cuando registro una venta a consumidor final de 1 "Yamaha" "FZ25" a 380000 cada una con forma de pago "<medio>"
    Entonces se genera una factura en estado "cobrada"
    Y la factura tiene un cobro de 380000 con medio de pago "<medio>"

    Ejemplos:
      | medio         |
      | Efectivo      |
      | Transferencia |
      | Crédito       |
      | Cheque        |

  @smoke @KAWA-48
  Escenario: Venta en cuenta corriente queda pendiente de cobro y debita la cuenta del cliente
    Dado que la cuenta corriente de "Moto Center" tiene saldo -45000
    Cuando registro una venta al cliente "Moto Center" de 1 "Yamaha" "FZ25" a 380000 cada una con forma de pago "Cuenta corriente"
    Entonces se genera una factura en estado "emitida"
    Y la factura no tiene cobros registrados
    Y el saldo pendiente de la factura es 380000
    Y el saldo de la cuenta corriente de "Moto Center" es -425000

  Escenario: El detalle de la factura copia los datos de la venta
    Cuando registro una venta al cliente "Motos Patagonia" de 3 "Motomel" "Skua 150" a 195000 cada una con forma de pago "Transferencia"
    Entonces el detalle de la factura tiene 1 línea
    Y la línea de la factura es 3 x "Motomel Skua 150" a 195000 con subtotal 585000
    Y la factura está a nombre de "Motos Patagonia SRL"

  Escenario: Las facturas se numeran en forma correlativa
    Cuando registro una venta a consumidor final de 1 "Zanella" "ZB 110" a 165000 cada una con forma de pago "Efectivo"
    Y registro una venta a consumidor final de 1 "Honda" "Wave 110" a 160000 cada una con forma de pago "Efectivo"
    Entonces la última factura tiene el número siguiente a la anterior

  @KAWA-46
  Escenario: Facturar las ventas históricas que todavía no tienen factura
    Dado que la cuenta corriente de "Moto Center" tiene saldo -45000
    Cuando facturo las ventas pendientes de facturar
    Entonces se generan 15 facturas
    Y no queda ninguna venta sin factura
    Y el saldo de la cuenta corriente de "Moto Center" es -45000
