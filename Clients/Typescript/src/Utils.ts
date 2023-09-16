export function Extend(obj, superCls)
{
    for (var key in superCls.prototype) {
        obj[key] = superCls.prototype[key];
    }
    return obj;
}