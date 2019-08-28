namespace Shared

type Length = Meter | Millimeter | Kilometer | USFoot

type Conversion = {
    Source: Length
    Target: Length
    Input: float
}

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type IConverterApi =
    { convert : Conversion -> Async<float> }