module Length

open Shared

let private get = function
| Meter -> 1.0
| Millimeter -> 1e-3
| Kilometer -> 1e3
| USFoot -> 0.3048

let convert conversion : float =
    let sourceFactor = get conversion.Source
    let targetFactor = get conversion.Target
    conversion.Input * sourceFactor / targetFactor