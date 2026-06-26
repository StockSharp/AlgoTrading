# Estratégia Sistema Aberto de Pivôs de Fuso Horário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão para a API de alto nível do StockSharp do expert do MetaTrader `Exp_TimeZonePivotsOpenSystem`. Reproduz a lógica original que ancora um canal de preço simétrico ao preço de abertura diário em uma hora configurável e reage quando velas completas rompem acima ou abaixo dessa faixa. Todas as ordens são enviadas como ordens de mercado e a proteção opcional de stop-loss / take-profit é configurada através do `StartProtection`.

## Como funciona

1. Assina o período de velas configurado, registra o passo de preço do instrumento e configura stops protetores se as distâncias forem maiores que zero.
2. Rastreia a primeira vela de cada dia cujo horário de abertura corresponde a `StartHour`. O preço de abertura dessa vela torna-se a âncora da sessão e define as faixas superior e inferior a `OffsetPoints` passos de preço acima e abaixo da âncora.
3. Calcula um sinal de cinco estados para cada vela finalizada, espelhando o buffer codificado por cores do indicador personalizado original:
   - `0` / `1`: a vela fechou acima da faixa superior (rompimento de alta, com o índice refletindo a direção da vela).
   - `2`: a vela terminou dentro da faixa (neutra).
   - `3` / `4`: a vela fechou abaixo da faixa inferior (rompimento de baixa).
4. Mantém um histórico deslizante de sinais. A vela localizada `SignalBar` passos atrás serve como barra de confirmação e a vela imediatamente anterior deve ser neutra para acionar uma entrada, recriando a lógica do MetaTrader que aguarda uma barra após o rompimento.
5. Quando aparece uma confirmação de alta, a estratégia opcionalmente fecha posições vendidas e, se estiver plana e permitido, abre uma nova posição comprada. As confirmações de baixa se comportam simetricamente para operações vendidas.
6. Após abrir uma nova posição, a estratégia adia novas entradas na mesma direção até que a próxima vela após a barra de confirmação comece, evitando ordens duplicadas na mesma sessão.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Período de velas para os cálculos de rompimento. | `H1` |
| `OrderVolume` | Volume usado para novas posições. | `0.1` |
| `StartHour` | Hora (0-23) cujo preço de abertura ancora as faixas diárias. | `0` |
| `OffsetPoints` | Meia-largura da faixa em passos de preço (unidades de tick). | `100` |
| `SignalBar` | Número de velas fechadas entre a barra atual e a confirmação do rompimento. Deve ser ≥ 1 nesta conversão. | `1` |
| `StopLossPoints` | Distância do stop protetor em passos de preço. | `1000` |
| `TakeProfitPoints` | Distância do alvo de lucro em passos de preço. | `2000` |
| `EnableLongEntry` | Permitir abertura de posições compradas após sinais de alta. | `true` |
| `EnableShortEntry` | Permitir abertura de posições vendidas após sinais de baixa. | `true` |
| `CloseLongOnBearishBreak` | Fechar posições compradas existentes em confirmações de baixa. | `true` |
| `CloseShortOnBullishBreak` | Fechar posições vendidas existentes em confirmações de alta. | `true` |

## Notas

- O bloco de gestão de dinheiro da versão MetaTrader é substituído pelo parâmetro explícito `OrderVolume` típico das estratégias StockSharp.
- Os parâmetros de stop-loss e take-profit são convertidos de distâncias em pontos para deslocamentos de preço absolutos usando o passo de preço atual do instrumento.
- A implementação S# mantém apenas uma posição líquida (comprada, vendida ou plana) exatamente como o original MQL, e pulará novas entradas enquanto uma posição ainda estiver aberta.
