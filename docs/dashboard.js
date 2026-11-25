const empty = document.getElementById("empty");
const errorBox = document.getElementById("error");
const errorText = document.getElementById("error-text");
const trends = document.getElementById("trends");
const tableWrap = document.getElementById("table-wrap");
const rows = document.getElementById("rows");

function cell(value) {
  const td = document.createElement("td");
  td.textContent = value === null || value === undefined || value === "" ? "—" : String(value);
  return td;
}

function series(runs, read) {
  return runs.map((run) => {
    const value = read(run);
    return Number.isFinite(value) ? value : null;
  });
}

function draw(container, values, className) {
  container.replaceChildren();
  const numbers = values.filter((value) => value !== null);
  const max = numbers.length ? Math.max(...numbers, 1) : 1;
  values.forEach((value) => {
    const bar = document.createElement("div");
    bar.className = "bar " + (value === null ? "missing" : className);
    if (value !== null) {
      bar.style.height = Math.max(4, (value / max) * 100) + "px";
      const label = document.createElement("span");
      label.textContent = String(value);
      bar.appendChild(label);
    }
    container.appendChild(bar);
  });
}

function render(runs) {
  if (!Array.isArray(runs) || runs.length === 0) {
    empty.hidden = false;
    return;
  }

  trends.hidden = false;
  tableWrap.hidden = false;
  draw(document.getElementById("duration"), series(runs, (run) => run.tests ? run.tests.durationSeconds : null), "");
  draw(document.getElementById("failed"), series(runs, (run) => run.tests ? run.tests.failed : null), "fail");
  draw(document.getElementById("warnings"), series(runs, (run) => run.validator ? run.validator.warnings : null), "warn");

  runs.forEach((run) => {
    const tr = document.createElement("tr");
    tr.appendChild(cell(run.recordedAtUtc));
    const sha = document.createElement("td");
    sha.textContent = run.commitSha ? String(run.commitSha).slice(0, 7) : "—";
    tr.appendChild(sha);
    const result = document.createElement("td");
    result.className = "result " + (run.result || "incomplete");
    result.textContent = run.result || "incomplete";
    tr.appendChild(result);
    tr.appendChild(cell(run.tests ? run.tests.passed : null));
    tr.appendChild(cell(run.tests ? run.tests.failed : null));
    tr.appendChild(cell(run.tests ? run.tests.skipped : null));
    tr.appendChild(cell(run.tests ? run.tests.durationSeconds : null));
    tr.appendChild(cell(run.validator ? run.validator.warnings : null));
    tr.appendChild(cell(run.validator ? run.validator.errors : null));
    rows.appendChild(tr);
  });
}

fetch("data/runs.json")
  .then((response) => {
    if (!response.ok) throw new Error("data/runs.json returned " + response.status);
    return response.json();
  })
  .then(render)
  .catch((error) => {
    errorBox.hidden = false;
    errorText.textContent = error.message;
  });
