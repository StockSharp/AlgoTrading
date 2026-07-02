# Estratégia de Reversão de Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Reversão de Gap** é uma porta direta do MetaTrader 4 consultor especialista `gaps.mq4`. O sistema monitora velas e loo de 15 minutos
ks para abertura de gaps que ocorrem fora da faixa máxima/baixa da vela anterior. Quando tal lacuna aparece, a estratégia imediatamente muda.
entra no mercado na direção do movimento de reversão à média esperado.

A versão StockSharp segue a lógica original enquanto depende da assinatura de vela de alto nível API. Toda a gestão comercial é
feito com ordens de mercado e nenhuma ordem de proteção fixa é colocada, refletindo o comportamento encontrado no código MQL.

## Regras de negociação

1. Assine velas de 15 minutos (configuráveis através do parâmetro `CandleType`).
2. Mantenha os valores máximos e mínimos da vela concluída anterior.
3. Quando uma nova vela começa:
   - Calcule o buffer de lacuna: `(MinGapSize + spreadInSteps) * pointValue`.
   - Se o preço de abertura estiver **acima** `previousHigh + gapBuffer`, abra uma posição **curta**.
   - Se o preço de abertura estiver **abaixo** de `previousLow - gapBuffer`, abra uma posição **longa**.
4. Apenas uma negociação por vela é permitida. Uma vez colocada uma ordem, a estratégia espera pela próxima vela antes de gerar uma nova.
sinal.

O componente de spread usa o melhor lance/venda atual, se disponível. Quando nenhum dado de cotação é fornecido, a estratégia cai em pecado
passo de preço como um buffer conservador.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `MinGapSize` | `1` | Tamanho mínimo do intervalo nas etapas de preço que deve ser excedido antes de enviar um pedido. |
| `GapVolume` | `0.1` | Volume de pedidos para entradas no mercado desencadeadas por lacunas. |
| `CandleType` | `15m TimeFrame` | Tipo de vela usado para cálculos (o padrão é velas de 15 minutos). |

Todos os parâmetros são registrados como `StrategyParam<T>` e suportam otimização dentro do StockSharp Designer ou outras ferramentas.

## Notas de implementação

- Usa `SubscribeCandles` com `Bind` para processar apenas velas concluídas.
- Lembra o intervalo da vela anterior para evitar o recálculo da série de dados.
- Bloqueia pedidos duplicados na mesma vela rastreando o tempo de abertura da barra que acionou a negociação.
- A saída do gráfico desenha as velas subscritas e as negociações estratégicas para uma rápida inspeção visual.

## Diferenças da versão MQL

- Os níveis de take-profit e stop-loss não foram definidos corretamente no EA original (o código MQL passou valores para os parâmetros errados)
. A porta StockSharp, portanto, mantém o comportamento de execução sem ordens de proteção.
- O gerenciamento de spread agora verifica cotações de compra/venda em tempo real quando disponíveis, fornecendo um buffer mais adaptável.

## Requisitos

- StockSharp API com acesso aos dados da vela para o instrumento selecionado.
- As cotações de nível 1 são opcionais, mas melhoram a detecção de spread.
