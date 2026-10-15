# Estratégia de Rede Neural ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia replica o consultor especialista "Neurotest", combinando um sistema neural leve
camada de rede com gerenciamento de dinheiro baseado em ATR dentro de StockSharp. O modelo consome o
última vela M15 concluída e a transforma em cinco recursos normalizados: perto de
impulso de fechamento, intervalo intradiário, corpo da vela, expansão de volume e volatilidade (ATR a
relação preço). Uma única camada oculta com saída sigmóide produz uma pontuação de probabilidade
que é dimensionado por uma taxa de aprendizagem dinâmica. A pontuação é comparada com a definida pelo usuário
limites de compra e venda para abrir ou inverter posições.

## Regras de negociação

1. Assine velas de 15 minutos (configuráveis) e calcule ATR do mesmo período.
2. Construa os cinco recursos normalizados da vela anterior e da atual finalizada
vela e, em seguida, avalie a rede neural.
3. Quando a previsão ajustada está acima do limite de compra e a posição atual é
não muito tempo, entre em uma negociação longa (fechando a exposição curta, se necessário).
4. Quando a previsão ajustada estiver abaixo do limite de venda e a posição atual for
não curto, entre em uma negociação curta.
5. Cada entrada anexa ordens de stop-loss e take-profit baseadas em ATR. Se ATR não for formado,
uma distância de retorno em pontos é usada.
6. Se o spread atual ultrapassar o limite configurado a vela será ignorada.

## Gestão de risco

- O tamanho da posição é calculado a partir do patrimônio do portfólio e da distância de stop ATR para que o
a perda no stop é igual a `Max Risk %` do patrimônio líquido.
- As ordens de proteção usam um multiplicador configurável de risco-recompensa.
- A negociação é interrompida automaticamente quando o rebaixamento diário ou total excede seus limites.
- Um sistema de penalidades diminui a taxa de aprendizagem em 10% (até um mínimo) quando o valor diário
a meta de lucro não é alcançada, o que amortece os sinais futuros até o próximo dia de negociação.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **% de risco máximo** | Risco máximo por negociação como percentagem do capital próprio. |
| **Perda diária%** | Limite de rebaixamento diário que interrompe a negociação. |
| **Perda Total%** | Limite de rebaixamento global que interrompe a negociação. |
| **% de lucro diário** | Meta de lucro diário antes que a penalidade seja ignorada. |
| **Taxa de aprendizagem** | Fator de escala aplicado à saída neural. |
| **Camada oculta** | Número de neurônios na camada oculta. |
| **Limite de compra/Limite de venda** | Níveis de gatilho para entradas longas e curtas. |
| **Tipo de vela** | Tipo de vela e prazo usado para sinais. |
| **ATR Período** | Período do indicador ATR. |
| **Espalhamento máximo** | Spread máximo permitido em etapas de preço. |
| **Recompensa de risco** | Multiplicador de take-profit em relação à distância de parada. |
| **Parada substituta** | Pare a distância em pontos quando ATR não estiver disponível. |

## Notas

- A assinatura Level1 é necessária para monitorar o spread de compra/venda antes de cada decisão.
- Os pesos da rede neural são inicializados aleatoriamente, mas determinísticos (semente 42). O
a modulação da taxa de aprendizagem emula o comportamento adaptativo do especialista MQL original.
- Certifique-se de que o portfólio conectado forneça `CurrentValue`, `StepPrice` e limites de volume
para que o dimensionamento da posição e as ordens de proteção funcionem corretamente.
