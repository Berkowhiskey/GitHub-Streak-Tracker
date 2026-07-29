"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { CheckIcon, CopyIcon } from "@/components/icons";

/**
 * Salt okunur bir kod alani ve yanindaki kopyalama butonu.
 */
export function CopyField({ label, value }: { label: string; value: string }) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Pano erisimi reddedilirse kullanici metni elle secebilir.
    }
  }

  return (
    <div className="space-y-1.5">
      <label className="text-xs font-medium text-muted-foreground">{label}</label>
      <div className="flex gap-2">
        <input
          readOnly
          value={value}
          onFocus={(e) => e.currentTarget.select()}
          className="min-w-0 flex-1 rounded-md border bg-muted/40 px-3 py-2 font-mono text-xs"
        />
        <Button
          variant="outline"
          size="icon"
          onClick={handleCopy}
          aria-label={`${label} kopyala`}
        >
          {copied ? (
            <CheckIcon className="h-4 w-4 text-emerald-500" />
          ) : (
            <CopyIcon className="h-4 w-4" />
          )}
        </Button>
      </div>
    </div>
  );
}
