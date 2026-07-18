# Parabolic SAR Estratégia de alerta de inversão (4164)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia reproduz o consultor especialista MetaTrader **pSAR_alert2** dentro da estrutura StockSharp. Ele monitora o indicador Parabolic SAR no instrumento e período selecionados. Sempre que o valor SAR passa de acima do preço de fechamento para abaixo dele (ou vice-versa), a estratégia gera um alerta informativo. Opcionalmente, pode enviar ordens de mercado na direção da virada para transformar o alerta em uma entrada automatizada.

## Lógica de negociação

1. Assine a série de velas configurada e calcule o indicador Parabolic SAR com as configurações de aceleração fornecidas.
2. Espere que cada vela termine para emular o tempo original EA.
3. Compare o valor do indicador com o fechamento da vela:
   - O anterior SAR acima do fechamento e o atual SAR abaixo do fechamento → **mudança de alta**.
   - O anterior SAR abaixo do fechamento e o atual SAR acima do fechamento → **inversão de baixa**.
4. Registre um alerta detalhado para cada virada. Quando a negociação automática estiver ativada, nivele qualquer exposição oposta e abra uma nova posição na direção do sinal usando ordens de mercado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Candle Type` | Prazo usado para construir velas e avaliar o indicador Parabolic SAR. |
| `SAR Step` | Fator de aceleração inicial passado para Parabolic SAR. |
| `SAR Max` | Fator máximo de aceleração do Parabolic SAR. |
| `Enable Auto Trading` | Quando `true`, as ordens de mercado são enviadas em cada alerta; quando `false`, apenas logs são gerados. |
| `Trade Volume` | Tamanho do pedido aplicado quando a negociação automática está habilitada. |

## Notas de conversão

- O script MetaTrader original dependia de `Sleep` para limitar a execução. StockSharp é orientado a eventos, portanto a estratégia reage a novas velas imediatamente, sem atrasos manuais.
- Os alertas são produzidos por meio de `AddInfoLog`, mantendo o comportamento original das notificações pop-up sem exigir componentes de UI adicionais.
- A negociação automática opcional é fornecida para integrar a lógica de alerta em fluxos de trabalho automatizados. Desative o parâmetro `Enable Auto Trading` para corresponder ao comportamento exato de MetaTrader.
- A implementação do Python é omitida intencionalmente conforme solicitado.
