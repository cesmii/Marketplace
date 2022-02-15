export async function login(dispatch, data) {
    try {
        dispatch({ type: 'LOGIN_SUCCESS', payload: data });
        localStorage.setItem('authTicket', JSON.stringify(data));
        return true;
    } catch (error) {
        dispatch({ type: 'LOGIN_ERROR', error: error });
    }
}

export async function logout(dispatch) {
    dispatch({ type: 'LOGOUT' });
    localStorage.removeItem('authTicket');
    return true;
}
