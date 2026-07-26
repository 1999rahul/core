import Select from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';

// To Do: Comptete this current filter

function ToDoFilter() {

    const [currentFilter,setCurrentFilter] = useState("All");

    function handleChange(e){
        const currentValue = e.target.value;
        setCurrentFilter(currentValue);
    }

    return <Select
        labelId="demo-simple-select-label"
        id="demo-simple-select"
        value={currentFilter}
        label="Tasks"
        onChange={handleChange}
    >
        <MenuItem value={"All"}>All</MenuItem>
        <MenuItem value={20}>Twenty</MenuItem>
        <MenuItem value={30}>Thirty</MenuItem>
    </Select>
}

export default ToDoFilter;