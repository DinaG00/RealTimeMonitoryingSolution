import React, { useEffect, useState } from 'react';
import { 
    Table, 
    TableBody, 
    TableCell, 
    TableContainer, 
    TableHead, 
    TableRow, 
    Paper,
    Typography 
} from '@mui/material';
import axios from 'axios';

function ClipboardLogs() {
    const [logs, setLogs] = useState([]);

    useEffect(() => {
        fetchLogs();
        const interval = setInterval(fetchLogs, 5000);
        return () => clearInterval(interval);
    }, []);

    const fetchLogs = async () => {
        try {
            const response = await axios.get('http://localhost:5001/logs/clipboard');
            setLogs(response.data);
        } catch (error) {
            console.error('Error fetching clipboard logs:', error);
        }
    };

    return (
        <div>
            <Typography variant="h4" gutterBottom>
                Clipboard Logs
            </Typography>
            <TableContainer component={Paper}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>ID</TableCell>
                            <TableCell>Content</TableCell>
                            <TableCell>Timestamp</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {logs.map((log) => (
                            <TableRow key={log.id}>
                                <TableCell>{log.id}</TableCell>
                                <TableCell>{log.content}</TableCell>
                                <TableCell>{log.timestamp}</TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        </div>
    );
}

export default ClipboardLogs; 