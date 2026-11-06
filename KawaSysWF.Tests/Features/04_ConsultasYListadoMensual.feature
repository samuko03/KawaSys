# language: es
@consultas @KAWA-50 @KAWA-52 @KAWA-53
Característica: Consulta de facturas y listado mensual
  Como responsable de Finanzas
  Quiero consultar las facturas pendientes y el listado de cada mes
  Para hacer el seguimiento de la cobranza

  Antecedentes:
    Dado que el sistema tiene los datos iniciales de KawaSys
    Y que se facturaron las ventas históricas

  @smoke
  Escenario: Ver solo las facturas pendientes de cobro
    Cuando consulto las facturas pendientes de cobro
    Entonces obtengo 4 facturas
    Y todas están en estado "emitida" o "parcial"

  Escenario: Filtrar las facturas de un cliente
    Cuando consulto las facturas del cliente "Moto Center"
    Entonces obtengo 3 facturas
    Y todas son del cliente "Moto Center SRL"

  Esquema del escenario: Totales del listado mensual
    Cuando genero el listado mensual de <mes> de 2025
    Entonces obtengo <cantidad> facturas
    Y el total facturado es <facturado>
    Y el total cobrado es <cobrado>
    Y el total pendiente es <pendiente>

    Ejemplos:
      | mes | cantidad | facturado | cobrado | pendiente |
      | 5   | 10       | 3845000   | 2880000 | 965000    |
      | 6   | 5        | 3060000   | 1860000 | 1200000   |
      | 7   | 0        | 0         | 0       | 0         |

  Escenario: El listado mensual se ordena por número de factura
    Cuando genero el listado mensual de 5 de 2025
    Entonces las facturas están ordenadas por número de menor a mayor
