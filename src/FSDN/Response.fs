namespace FSDN

open System.Runtime.Serialization

[<DataContract>]
type Paging<'T> = {
  [<field: DataMember(Name = "values")>]
  Values: 'T []
}

[<DataContract>]
type RedirectPaging<'T> = {
  [<field: DataMember(Name = "values")>]
  Values: 'T []
  [<field: DataMember(Name = "path")>]
  Path: string
}
