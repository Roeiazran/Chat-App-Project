import { useState } from "react";
import { extractErrorMessages, getMessagesReport } from "../services/HttpService"
import type { HourReports } from "../types";
import { useErrorAndLoading } from "../hooks/useErrorAndLoading";

const MessagesReport: React.FC = () => {
    const [date, setDate] = useState<string>(() => new Date().toISOString().slice(0,10));
    const { loading, errors,  updateErrors, startLoad, finishLoad } = useErrorAndLoading();
    const [reports, setReports] = useState<HourReports[]>([]);

    // called to fetch messages reports
    const loadReport = async () => {

        try {
            startLoad();

            // fetch the message report from the server
            const res: HourReports[] = await getMessagesReport(date);  
            
            setReports(res);
        } catch (err) {
            const errors: string[] = extractErrorMessages(err);
            updateErrors(errors);
        } finally {
            finishLoad();
        }
    }

    return (
        <div>
            <h3>Average Message Length — per hour</h3>
            {/* input */}
            <div>
                <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                />
                <button onClick={loadReport} disabled={loading}>
                {loading ? "Loading…" : "Load"}
                </button>
            </div>
             {errors.map((msg, index) => <p key={index} style={{ color: "red" }}>{msg}</p>)}
            {/* reports */}
            <table>
                <thead>
                <tr>
                    <th>Hour</th>
                    <th>Average length</th>
                </tr>
                </thead>
                <tbody>
                    {reports.map((report) => (
                        <tr key={report.hour}>
                            <td>{report.hour}</td>
                            <td>{Number(report.avgMessageLength.toFixed(2))}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );

}


export default MessagesReport;