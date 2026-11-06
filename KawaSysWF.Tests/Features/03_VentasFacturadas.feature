# language: es
@facturacion @KAWA-51
Característica: Una venta facturada no se modifica ni se elimina
  Como responsable de Finanzas
  Quiero que una factura emitida cierre la operación de venta
  Para que lo facturado coincida siempre con lo vendido

  Antecedentes:
    Dado que el sistema tiene los datos iniciales de KawaSys

  @negativo @smoke
  Escenario: Intentar modificar una venta facturada
    Dado una venta facturada de 1 "Honda" "CB190" a 320000
    Cuando intento cambiar la cantidad de la venta a 5
    Entonces el sistema rechaza la operación con el mensaje "ya tiene emitida la factura"
    Y la venta sigue teniendo cantidad 1 y total 320000

  @negativo
  Escenario: Intentar eliminar una venta facturada
    Dado una venta facturada de 1 "Honda" "CB190" a 320000
    Cuando intento eliminar la venta
    Entonces el sistema rechaza la operación con el mensaje "ya tiene emitida la factura"
    Y la venta sigue existiendo

  Escenario: Una venta histórica sin factura todavía se puede modificar
    Dado la venta histórica número 2, que no tiene factura
    Cuando intento cambiar la cantidad de la venta a 2
    Entonces la operación se realiza sin errores
    Y la venta sigue teniendo cantidad 2 y total 440000

  Escenario: Una venta histórica sin factura todavía se puede eliminar
    Dado la venta histórica número 2, que no tiene factura
    Cuando intento eliminar la venta
    Entonces la operación se realiza sin errores
    Y la venta ya no existe
