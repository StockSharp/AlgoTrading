# Estratégia de interrupção de sessão TCPivot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia TCPivot Session Stop é uma porta direta do MetaTrader 4 consultor especialista `gpfTCPivotStop`. Ele é negociado em torno do nível de pivô diário clássico calculado a partir do dia de negociação anterior. A estratégia:

- Calcula o ponto de articulação, três níveis de resistência e três níveis de suporte da máxima, mínima e fechamento do dia anterior.
- Aguarda que o fechamento da corrente cruze o nível do pivô por baixo (configuração longa) ou por cima (configuração curta).
- Abre uma posição de mercado na direção do rompimento e atribui um stop-loss e um take-profit no nível de pivô selecionado.
- Opcionalmente, força o fechamento da posição no início de uma hora de sessão especificada para emular a saída intradiária original.

A implementação é baseada no StockSharp API de alto nível. As posições são dimensionadas com a propriedade `Volume` da classe base `Strategy`.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `TargetLevel` | Nível dinâmico usado para stop-loss e take-profit (1, 2 ou 3). | `1` |
| `CloseAtSessionStart` | Se habilitado, fecha as posições abertas quando a hora configurada começa. | `false` |
| `SessionCloseHour` | Hora da sessão (0-23) avaliada quando `CloseAtSessionStart` está ativado. | `0` |
| `CandleType` | Período de tempo que alimenta os sinais de negociação. | `H1` |

## Lógica de negociação

1. Assine velas horárias (ou configuradas) para sinais e velas diárias para cálculo de pivô.
2. Ao final de cada vela diária, calcule os níveis de pivô clássicos:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 * Pivot - Low`, `S1 = 2 * Pivot - High`
   - `R2 = Pivot + (R1 - S1)`, `S2 = Pivot - (R1 - S1)`
   - `R3 = High + 2 * (Pivot - Low)`, `S3 = Low - 2 * (High - Pivot)`
3. Quando uma vela sinalizadora termina:
   - Se `CloseAtSessionStart` estiver ativado e a vela abrir em `SessionCloseHour`, nivele imediatamente a posição.
   - Se estiver estável e o fechamento anterior estiver abaixo do pivô enquanto o fechamento atual estiver acima dele, insira comprado com alvo/stop selecionado por `TargetLevel`.
   - Se estiver estável e o fechamento anterior estiver acima do pivô enquanto o fechamento atual estiver abaixo dele, entre em posição curta com o alvo/stop espelhado.
   - Se já estiver em uma posição, saia quando o fechamento atingir o nível configurado de stop-loss ou take-profit.

## Notas

- A estratégia usa `StartProtection()` para integração com os controles de risco integrados da plataforma. As saídas stop-loss e take-profit são tratadas explicitamente dentro da lógica da estratégia.
- A versão MetaTrader incluía notificações opcionais por e-mail e dimensionamento dinâmico de posição com base no risco da conta. Esses recursos não fazem parte da porta StockSharp; use os módulos de notificação e gerenciamento de dinheiro da plataforma, se necessário.
- O consultor especialista original fechou as negociações à meia-noite quando `isTradeDay` estava ativado. Este comportamento é reproduzido por meio de `CloseAtSessionStart` + `SessionCloseHour` (definido como `0` para imitar meia-noite).
