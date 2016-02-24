namespace FSDN

open System.Runtime.Serialization

[<DataContract>]
type Paging<'T> = {
  [<field: DataMember(Name = "values")>]
  Values: 'T []
}
