# Estratégia de Quatro Telas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Quatro Telas opera usando velas Heikin-Ashi em quatro períodos: 5, 15, 30 e 60 minutos.
Entra comprada quando todos os períodos mostram velas altistas e entra vendida quando todos mostram velas baixistas.
Os níveis de stop-loss e take-profit são definidos em pontos com trailing stop opcional.

## Como funciona
1. Assina fluxos de velas de 5, 15, 30 e 60 minutos.
2. Calcula a abertura e o fechamento Heikin-Ashi para cada vela.
3. Marca cada período como altista ou baixista.
4. Entra comprada quando todos são altistas, entra vendida quando todos são baixistas.
5. Usa `StartProtection` para aplicar stop-loss, take-profit e trailing opcional.

## Parâmetros
- `CandleType` – período base para velas de 5 minutos.
- `StopLossPoints` – distância do stop-loss em pontos.
- `TakeProfitPoints` – distância do take-profit em pontos.
- `UseTrailing` – habilitar trailing stop (true/false).

O volume de negociação é definido pela propriedade `Volume` da estratégia.

## Notas
- Funciona com a API de alto nível usando `SubscribeCandles` e `Bind`.
- Processa apenas velas finalizadas.
- Os comentários no código estão em inglês.
