#!/bin/bash
# Batch test strategies. Usage: ./run_batch.sh [start_index] [count]
# Reads strategy list, swaps csproj+Program.cs, builds, runs, logs results.

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_DIR="$SCRIPT_DIR/../API"
CSPROJ="$SCRIPT_DIR/Runner.csproj"
PROGRAM="$SCRIPT_DIR/Program.cs"
RESULTS="$SCRIPT_DIR/results.log"
PLAN="$SCRIPT_DIR/../PLAN.md"

# Convert to Windows paths for Python
W_CSPROJ=$(cygpath -w "$CSPROJ")
W_PROGRAM=$(cygpath -w "$PROGRAM")

START=${1:-0}
COUNT=${2:-9999}

# Collect all strategy dirs
DIRS=($(ls -d "$API_DIR"/*/CS/ 2>/dev/null | sort))
TOTAL=${#DIRS[@]}

echo "Total strategies: $TOTAL, starting at index $START, count $COUNT"

# Shutdown build server to prevent file locking
dotnet build-server shutdown 2>/dev/null

# Helper: update Program.cs with new class name using python
update_program() {
    local cn="$1"
    python -c "
import re
p = r'${W_PROGRAM}'.replace('\\\\', '/')
with open(p, 'r') as f:
    c = f.read()
c = re.sub(r'var strategy = new [A-Za-z0-9_]+\(\);', 'var strategy = new ${cn}();', c)
with open(p, 'w') as f:
    f.write(c)
"
}

# Helper: update csproj Compile Include using python
update_csproj() {
    local rp="$1"
    python -c "
import re
p = r'${W_CSPROJ}'.replace('\\\\', '/')
with open(p, 'r') as f:
    c = f.read()
c = re.sub(r'<Compile Include=\"[^\"]*\" Link=\"Strategy.cs\" />', '<Compile Include=\"${rp}\" Link=\"Strategy.cs\" />', c)
with open(p, 'w') as f:
    f.write(c)
"
}

DONE=0
OK_COUNT=0
FAIL_LIST=""

for ((i=START; i<TOTAL && DONE<COUNT; i++)); do
    CS_DIR="${DIRS[$i]}"
    DIR_NAME=$(basename "$(dirname "$CS_DIR")")
    CS_FILE=$(ls "$CS_DIR"*.cs 2>/dev/null | head -1)

    if [ -z "$CS_FILE" ]; then
        echo "SKIP $DIR_NAME — no .cs file"
        continue
    fi

    # Skip if already processed in results (any status)
    if grep -q "^${DIR_NAME}|" "$RESULTS" 2>/dev/null; then
        if grep -q "^${DIR_NAME}|.*|OK|" "$RESULTS" 2>/dev/null; then
            OK_COUNT=$((OK_COUNT+1))
        fi
        DONE=$((DONE+1))
        continue
    fi

    FILE_NAME=$(basename "$CS_FILE" .cs)
    # Extract actual class name from the file
    CLASS_NAME=$(grep -oP 'public class \K[A-Za-z0-9_]+(?=\s*:)' "$CS_FILE" | head -1)
    if [ -z "$CLASS_NAME" ]; then
        CLASS_NAME="$FILE_NAME"
    fi
    REL_PATH="../API/$DIR_NAME/CS/$FILE_NAME.cs"

    echo -n "[$i/$TOTAL] $DIR_NAME — $CLASS_NAME ... "

    # Update csproj and Program.cs
    update_csproj "$REL_PATH"
    update_program "$CLASS_NAME"

    # Verify the replacement worked
    if ! grep -q "new ${CLASS_NAME}()" "$PROGRAM"; then
        echo "UPDATE_FAIL"
        echo "$DIR_NAME|$CLASS_NAME|BUILD_FAIL|Program.cs update failed" >> "$RESULTS"
        FAIL_LIST="$FAIL_LIST $DIR_NAME"
        DONE=$((DONE+1))
        continue
    fi

    # Build (with retry on file lock)
    BUILD_OK=0
    for attempt in 1 2 3; do
        BUILD_OUT=$(cd "$SCRIPT_DIR" && timeout 90 dotnet build Runner.csproj -v q --no-incremental -p:UseSharedCompilation=false 2>&1)
        BUILD_ERRORS=$(echo "$BUILD_OUT" | grep -c "error CS")

        if [ "$BUILD_ERRORS" -eq 0 ]; then
            BUILD_OK=1
            break
        fi

        # Check if it's a file lock error
        FILE_LOCK=$(echo "$BUILD_OUT" | grep -c "CS2012")
        if [ "$FILE_LOCK" -gt 0 ] && [ "$attempt" -lt 3 ]; then
            echo -n "(retry $attempt) "
            dotnet build-server shutdown 2>/dev/null
            sleep 3
        else
            break
        fi
    done

    if [ "$BUILD_OK" -eq 0 ]; then
        echo "BUILD_FAIL ($BUILD_ERRORS errors)"
        ERRORS=$(echo "$BUILD_OUT" | grep "error CS" | head -3)
        echo "$DIR_NAME|$CLASS_NAME|BUILD_FAIL|$ERRORS" >> "$RESULTS"
        FAIL_LIST="$FAIL_LIST $DIR_NAME"
        DONE=$((DONE+1))
        continue
    fi

    # Run with 60s timeout
    RUN_OUT=$(cd "$SCRIPT_DIR" && timeout 60 dotnet run --project Runner.csproj --no-build 2>&1)
    RUN_EXIT=$?

    ORDERS=$(echo "$RUN_OUT" | grep "^Orders:" | awk '{print $2}')
    TRADES=$(echo "$RUN_OUT" | grep "^Trades:" | awk '{print $2}')
    PNL=$(echo "$RUN_OUT" | grep "^PnL:" | awk '{print $2}')
    STATUS=$(echo "$RUN_OUT" | grep -oE "^(OK|FAIL|TIMEOUT)" | head -1)

    if [ -z "$STATUS" ]; then
        STATUS="TIMEOUT"
    fi

    echo "$STATUS (orders=$ORDERS trades=$TRADES pnl=$PNL)"
    echo "$DIR_NAME|$CLASS_NAME|$STATUS|orders=$ORDERS|trades=$TRADES|pnl=$PNL" >> "$RESULTS"

    # Mark in PLAN.md if OK
    if [ "$STATUS" = "OK" ]; then
        sed -i "s/- \[ \] $DIR_NAME/- [x] $DIR_NAME/" "$PLAN"
        OK_COUNT=$((OK_COUNT+1))
    else
        FAIL_LIST="$FAIL_LIST $DIR_NAME"
    fi

    DONE=$((DONE+1))
done

echo ""
echo "Done: $DONE strategies processed, $OK_COUNT OK"
if [ -n "$FAIL_LIST" ]; then
    echo "Failed:$FAIL_LIST"
fi
