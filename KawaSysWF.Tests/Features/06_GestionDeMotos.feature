# language: es
@motos
Característica: Gestión de modelos de motos
  Como usuario de Compras de Kawa
  Quiero registrar, modificar, dar de baja y consultar los modelos de motos con su stock
  Para que las ventas tomen los datos de un catálogo único en lugar de cargarlos a mano

  Antecedentes:
    Dado que el catálogo tiene los modelos iniciales de KawaSys

  # ── RD: Registrar modelos de motos ──────────────────────────────────────
  @smoke @registrar
  Escenario: Registrar un modelo nuevo
    Cuando registro el modelo "Kawasaki" "Z400" de 400 cc color "Verde" a 1200000 con stock 3
    Entonces el modelo "Kawasaki" "Z400" figura en el catálogo como activo
    Y el modelo "Kawasaki" "Z400" tiene precio 1200000 y stock 3

  @registrar
  Escenario: No se permite registrar un modelo que ya existe
    Cuando registro el modelo "honda" "xr150" de 150 cc color "Rojo" a 285000 con stock 1
    Entonces el sistema rechaza la operación con el mensaje "Ya existe un modelo activo"

  @registrar
  Esquema del escenario: No se permite registrar un modelo con datos inválidos
    Cuando registro el modelo "<marca>" "<modelo>" de 150 cc color "Negro" a <precio> con stock <stock>
    Entonces el sistema rechaza la operación con el mensaje "<mensaje>"

    Ejemplos:
      | marca   | modelo  | precio | stock | mensaje                          |
      |         | RX 150  | 200000 | 1     | La marca y el modelo son obligatorios |
      | Zanella |         | 200000 | 1     | La marca y el modelo son obligatorios |
      | Zanella | RX 150  | 0      | 1     | El precio debe ser mayor a cero  |
      | Zanella | RX 150  | 200000 | -1    | El stock no puede ser negativo   |

  # ── RD: Modificar los datos de modelos registrados ──────────────────────
  @smoke @modificar
  Escenario: Modificar el precio y el color de un modelo
    Cuando modifico el modelo "Honda" "CB190" con precio 335000 y color "Gris"
    Entonces el modelo "Honda" "CB190" tiene precio 335000 y stock 4
    Y el modelo "Honda" "CB190" es de color "Gris"

  @modificar
  Escenario: No se permite modificar un modelo para que quede igual a otro
    Cuando cambio el nombre del modelo "Honda" "CB190" a "XR150"
    Entonces el sistema rechaza la operación con el mensaje "Ya existe un modelo activo"

  # ── RD: Dar de baja modelos de motos ────────────────────────────────────
  @smoke @baja
  Escenario: Dar de baja un modelo lo saca del catálogo activo pero lo conserva
    Cuando doy de baja el modelo "Zanella" "Due 200"
    Entonces el modelo "Zanella" "Due 200" no figura entre los modelos activos
    Y el modelo "Zanella" "Due 200" figura en el catálogo como dado de baja

  @baja
  Escenario: No se puede vender un modelo dado de baja
    Dado que el modelo "Zanella" "Due 200" está dado de baja
    Cuando intento vender 1 "Zanella" "Due 200" a 280000
    Entonces el sistema rechaza la operación con el mensaje "está dado de baja"
    Y siguen registradas 15 ventas

  @baja
  Escenario: Reactivar un modelo dado de baja
    Dado que el modelo "Zanella" "Due 200" está dado de baja
    Cuando reactivo el modelo "Zanella" "Due 200"
    Entonces el modelo "Zanella" "Due 200" figura en el catálogo como activo

  # ── RD: Consultar modelos por marca o modelo ────────────────────────────
  @smoke @consultar
  Esquema del escenario: Consultar modelos por marca o modelo
    Cuando busco modelos con el texto "<texto>"
    Entonces obtengo <cantidad> modelos
    Y todos los modelos encontrados contienen "<texto>"

    Ejemplos:
      | texto     | cantidad |
      | Honda     | 5        |
      | yamaha    | 4        |
      | 150       | 3        |
      | Yamaha FZ | 1        |
      | Ducati    | 0        |

  # ── RD: Actualizar el stock de cada modelo ──────────────────────────────
  @stock
  Escenario: Ingresar unidades suma al stock
    Cuando ingreso 5 unidades del modelo "Kawasaki" "Ninja 300"
    Entonces el modelo "Kawasaki" "Ninja 300" tiene stock 5

  @stock
  Escenario: Un ajuste no puede dejar el stock negativo
    Cuando resto 3 unidades del modelo "Yamaha" "MT-03"
    Entonces el sistema rechaza la operación con el mensaje "no puede quedar negativo"
    Y el modelo "Yamaha" "MT-03" tiene stock 2

  @smoke @stock
  Escenario: Registrar una venta descuenta el stock del modelo
    Cuando intento vender 2 "Honda" "XR150" a 285000
    Entonces la operación se realiza sin errores
    Y el modelo "Honda" "XR150" tiene stock 6

  @stock
  Escenario: No se puede vender más unidades que el stock disponible
    Cuando intento vender 3 "Yamaha" "MT-03" a 890000
    Entonces el sistema rechaza la operación con el mensaje "Stock insuficiente"
    Y el modelo "Yamaha" "MT-03" tiene stock 2
    Y siguen registradas 15 ventas
