//import { AppSettings } from "../../utils/appsettings";
//import { generateLogMessageString } from "../../utils/UtilityService";

//const CLASS_NAME = "CartUtil";

//-------------------------------------------------------------------
// add to cart
//-------------------------------------------------------------------
export function updateCart(cart, item, quantity) {

    if (cart == null) cart = {};
    if (cart.items == null) cart.items = [];

    //loop through cart and see if item is there. If not, add, if there, add new quantity to existing.
    let cartItem = cart.items.find(x => { return x.marketplaceItem?.id === item.marketplaceItem?.id; });
    if (cartItem == null) {
        cartItem = { marketplaceItem: item.marketplaceItem, quantity: quantity };
        cart.items.push(cartItem);
    }
    else {
        cartItem.quantity += quantity;
    }
    return cart;
}

//-------------------------------------------------------------------
// get cart count
//-------------------------------------------------------------------
export function getCartCount(cart) {

    if (cart?.items == null) return 0;
    let result = 0;
    cart.items.forEach(x => { result += x.quantity ?? 0; });
    return result;
}

//-------------------------------------------------------------------
// get cart item count
//-------------------------------------------------------------------
export function getCartItemCount(cart, marketplaceItemId) {

    if (cart == null) cart = {};
    if (cart.items == null) cart.items = [];

    //loop through cart and see if item is there. 
    let cartItem = cart.items.find(x => { return x.marketplaceItem?.id === marketplaceItemId; });
    if (cartItem == null) return 0;
    return cartItem.quantity;
}

//-------------------------------------------------------------------
// get cart item count
//-------------------------------------------------------------------
export function removeCartItem(cart, marketplaceItemId) {

    if (cart == null) cart = {};
    if (cart.items == null) cart.items = [];

    //loop through cart and see if item is there. 
    let x = cart.items.findIndex(x => { return x.marketplaceItem?.id === marketplaceItemId; });
    if (x < 0) return;
    cart.splice(x, 1);
    return cart;
}

//-------------------------------------------------------------------
// validate cart - make sure all items can be purchased and all quantities are > 0
//-------------------------------------------------------------------
export function validateCart(cart) {

    if (cart == null) return false;
    if (cart.items == null) return false;

    //loop through cart and see if item is there. 
    let result = {quantity: true, allowPurchase: true};
    let x = cart.items.forEach(item => {
        //check item is allowed to be purchased
        if (!item.marketplaceItem.allowPurchase)
        {
            result = { ...result, allowPurchase: false };
        }
        //check for valid qty
        var isValid = validateCartItem_Quantity(item.quantity);
        if (!isValid.required || isValid.numeric || isValid.range) {
            result = { ...result, quantity: false };
        }
    });

    return result;
}

export const validateCartItem_Quantity = (val) => {
    var required = (val != null);
    var numeric = required && (!isNaN(parseInt(val)));
    var range = required && parseInt(val) > 0;
    return { required: required, numeric: numeric, range: range };
};
