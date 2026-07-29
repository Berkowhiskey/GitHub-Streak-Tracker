"use client";

import { useMemo } from "react";
import type { CalendarDay } from "@/lib/api";

/** Katki yogunluguna gore renk siniflari (alev temasiyla uyumlu). */
const LEVEL_CLASSES = [
  "bg-muted",
  "bg-orange-900/60",
  "bg-orange-700/70",
  "bg-orange-500/80",
  "bg-orange-400",
];

function levelFor(count: number): number {
  if (count === 0) return 0;
  if (count <= 2) return 1;
  if (count <= 5) return 2;
  if (count <= 9) return 3;
  return 4;
}

const DAY_LABELS = ["Pzt", "Çar", "Cum"];

export function ContributionHeatmap({ days }: { days: CalendarDay[] }) {
  // Gunleri haftalara boluyoruz: her sutun bir hafta, her satir haftanin bir gunu.
  const weeks = useMemo(() => {
    if (days.length === 0) return [];

    const sorted = [...days].sort((a, b) => a.date.localeCompare(b.date));
    const result: (CalendarDay | null)[][] = [];

    // Ilk haftanin basindaki bos gunler icin dolgu ekle (haftalar Pazar'dan baslar).
    const firstDayOfWeek = new Date(sorted[0].date + "T00:00:00Z").getUTCDay();
    let current: (CalendarDay | null)[] = Array(firstDayOfWeek).fill(null);

    for (const day of sorted) {
      current.push(day);

      if (current.length === 7) {
        result.push(current);
        current = [];
      }
    }

    if (current.length > 0) {
      while (current.length < 7) current.push(null);
      result.push(current);
    }

    return result;
  }, [days]);

  const total = useMemo(
    () => days.reduce((sum, day) => sum + day.contributionCount, 0),
    [days],
  );

  if (weeks.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        Henuz gosterilecek katki verisi yok.
      </p>
    );
  }

  return (
    <div className="space-y-3">
      <p className="text-sm text-muted-foreground">
        Son bir yilda <strong className="text-foreground">{total}</strong> katki
      </p>

      {/* Genis icerik dar ekranlarda yatay kayar; sayfa govdesi kaymaz. */}
      <div className="overflow-x-auto pb-2">
        <div className="flex gap-3">
          <div className="flex flex-col justify-between py-[2px] text-[10px] text-muted-foreground">
            {DAY_LABELS.map((label) => (
              <span key={label}>{label}</span>
            ))}
          </div>

          <div className="flex gap-[3px]">
            {weeks.map((week, weekIndex) => (
              <div key={weekIndex} className="flex flex-col gap-[3px]">
                {week.map((day, dayIndex) => (
                  <div
                    key={dayIndex}
                    className={`h-[11px] w-[11px] rounded-[2px] ${
                      day ? LEVEL_CLASSES[levelFor(day.contributionCount)] : "bg-transparent"
                    }`}
                    title={
                      day
                        ? `${formatDate(day.date)}: ${day.contributionCount} katki`
                        : undefined
                    }
                  />
                ))}
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="flex items-center gap-2 text-[10px] text-muted-foreground">
        <span>Az</span>
        {LEVEL_CLASSES.map((className, index) => (
          <div key={index} className={`h-[11px] w-[11px] rounded-[2px] ${className}`} />
        ))}
        <span>Cok</span>
      </div>
    </div>
  );
}

function formatDate(isoDate: string): string {
  return new Date(isoDate + "T00:00:00Z").toLocaleDateString("tr-TR", {
    day: "numeric",
    month: "short",
    timeZone: "UTC",
  });
}
