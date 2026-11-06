# language: es
@cobranzas @KAWA-47 @KAWA-48
Característica: Registro de cobros sobre facturas pendientes
  Como responsable de Finanzas
  Quiero registrar los cobros parciales o totales de las facturas en cuenta corriente
  Para saber cuánto debe cada cliente y mantener su cuenta corriente al día

  Antecedentes:
    Dado que el sistema tiene los datos iniciales de KawaSys
    Y una factura en cuenta corriente de "Distribuidora Del Sur" por 600000

  @smoke
  Escenario: Cobro parcial deja la factura en estado parcial
    Cuando registro un cobro de 200000 con "Transferencia"
    Entonces la factura queda en estado "parcial"
    Y el saldo pendiente de la factura es 400000
    Y el saldo de la cuenta corriente de "Distribuidora Del Sur" es -387500

  @smoke
  Escenario: Cobro por el total pendiente deja la factura cobrada
    Cuando registro un cobro de 600000 con "Cheque"
    Entonces la factura queda en estado "cobrada"
    Y el saldo pendiente de la factura es 0
    Y el saldo de la cuenta corriente de "Distribuidora Del Sur" es 12500

  Escenario: Varios cobros parciales completan la factura
    Cuando registro un cobro de 250000 con "Efectivo"
    Y registro un cobro de 350000 con "Transferencia"
    Entonces la factura queda en estado "cobrada"
    Y la factura tiene 2 cobros registrados

  @negativo
  Escenario: No se puede cobrar más que el saldo pendiente
    Cuando registro un cobro de 650000 con "Efectivo"
    Entonces el sistema rechaza la operación con el mensaje "supera el saldo pendiente"
    Y la factura queda en estado "emitida"
    Y la factura no tiene cobros registrados

  @negativo
  Esquema del escenario: El monto del cobro tiene que ser mayor a cero
    Cuando registro un cobro de <monto> con "Efectivo"
    Entonces el sistema rechaza la operación con el mensaje "mayor a cero"

    Ejemplos:
      | monto  |
      | 0      |
      | -1000  |

  @negativo
  Escenario: Solo se aceptan los medios de pago habilitados
    Cuando registro un cobro de 100000 con "Bitcoin"
    Entonces el sistema rechaza la operación con el mensaje "Medio de pago inválido"

  @negativo
  Escenario: No se puede cobrar una factura que no existe
    Cuando registro un cobro de 100000 con "Efectivo" sobre la factura 9999
    Entonces el sistema rechaza la operación con el mensaje "La factura no existe"
