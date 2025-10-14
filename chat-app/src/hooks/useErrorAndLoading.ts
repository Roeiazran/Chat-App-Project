import { useState, useCallback } from "react";


export function useErrorAndLoading() {
    const [errors, setErrors] = useState<string[]>([]);
    const [loading, setLoading] = useState<boolean>(false);

    const startLoad = useCallback(()=> {
        setLoading(true);
    }, []);

    const finishLoad = useCallback(()=> {
        setLoading(false);
    }, []);

    const appendError = useCallback((error: string) => {
        setErrors(prev => [...prev, error]);
    }, []);

    const updateErrors = useCallback((errors: string[])=> {
        setErrors(errors);
    }, []);

    const clearErrors = useCallback(()=> {
        setErrors([]);
    }, []);


    return { errors, loading, appendError, clearErrors, startLoad, finishLoad, updateErrors };
}