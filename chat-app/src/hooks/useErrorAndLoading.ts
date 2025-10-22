import { useState } from "react";

export function useErrorAndLoading() {
    const [errors, setErrors] = useState<string[]>([]);
    const [loading, setLoading] = useState<boolean>(false);

    const startLoad = ()=> {
        setLoading(true);
    };

    const finishLoad = ()=> {
        setLoading(false);
    };

    const appendError = (error: string) => {
        setErrors(prev => [...prev, error]);
    };

    const updateErrors = (errors: string[])=> {
        setErrors(errors);
    };

    const clearErrors = ()=> {
        setErrors([]);
    };

    return { errors, loading, appendError, clearErrors, startLoad, finishLoad, updateErrors };
}