import { useEffect, useState } from "react";

// For getting the data, later generalise it for the production grade
function useFetch(url){
    const [loading, setLoading] = useState(null);
    const [error, setError] = useState(null);
    const [data, setData] = useState(null);

    useEffect(() => {
        async function getData(){
            try{
                setLoading(true);
                setError(null);

                const response = await fetch(url);  
                if (!response.ok){
                    throw new Error("Request failed");
                }

                const parsedData = await response.json();
                setLoading(false);
                setData(parsedData)
                console.log(parsedData)
            }
            catch(err){
                setLoading(false);
                setError(err);
                setData(null);
            }
        }
        getData();
    }, [url]);

    return [loading, error, data]
}

export default useFetch;