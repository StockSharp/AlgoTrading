# Estratégia TemplateEAbyMarket
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
TemplateEAbyMarket é uma versão direta StockSharp do consultor especialista original MetaTrader 4 *TemplateEAbyMarket.mq4*. A estratégia usa o indicador Moving Average Convergence Divergence (MACD) para detectar mudanças de impulso. Quando a linha principal MACD cruza a linha de sinal enquanto ambos os componentes estão na mesma zona positiva ou negativa, a estratégia abre uma posição de mercado na direção do cruzamento. As saídas são gerenciadas exclusivamente por meio de ordens de proteção (takeprofit e stop loss) configuradas por meio do auxiliar `StartProtection` integrado.

A versão StockSharp mantém o comportamento do programa MQL: apenas abre novas posições sem tentar fechar automaticamente o lado oposto. Uma vez preenchida uma posição, a negociação fica a ser gerida por níveis de proteção ou intervenção manual.

## Lógica de negociação
1. Assine o tipo de vela selecionado pelo usuário (padrão: período de 15 minutos).
2. Calcule MACD (26/12/9 por padrão) em cada vela concluída.
3. Rastreie a posição relativa das linhas principal e de sinal MACD para detectar um evento de cruzamento:
   - **Configuração de alta:** a vela anterior tinha a linha principal abaixo da linha de sinal, a vela atual fecha com a linha principal acima da linha de sinal e ambas as linhas estão acima de zero. Uma ordem de compra a mercado com `OrderVolume` será enviada se a exposição atual for inferior a `MaxOrders * OrderVolume`.
   - **Configuração de baixa:** a vela anterior tinha a linha principal acima da linha de sinal, a vela atual fecha com a linha principal abaixo da linha de sinal e ambas as linhas estão abaixo de zero. Uma ordem de venda a mercado com `OrderVolume` é submetida sujeita ao mesmo limite de exposição.
4. Os níveis de proteção `takeProfit` e `stopLoss` são ativados uma vez na inicialização. A estratégia não fecha posições opostas automaticamente; o risco é controlado pelo módulo de proteção ou pelo usuário.

## Parâmetros
| Nome | Descrição |
|------|-------------|
| `MacdFastPeriod` | Comprimento EMA rápido para o cálculo de MACD. |
| `MacdSlowPeriod` | Comprimento EMA lento para o cálculo MACD. |
| `MacdSignalPeriod` | Comprimento do sinal EMA para o cálculo MACD. |
| `CandleType` | Tipo de vela (período de tempo) que alimenta o indicador. |
| `OrderVolume` | Volume enviado com cada ordem de mercado. |
| `MaxOrders` | Número máximo de pedidos simultâneos, expresso como múltiplos de `OrderVolume`. A estratégia verifica `abs(Position) < MaxOrders * OrderVolume` antes de enviar um novo pedido. |
| `TakeProfitPoints` | Distância de lucro em faixas de preço. O valor `0` desativa o take-profit. |
| `StopLossPoints` | Distância de stop-loss em faixas de preço. O valor `0` desativa o stop loss. |

## Notas
- As configurações de deslizamento e número mágico da versão MQL são omitidas intencionalmente porque são tratadas de forma diferente em StockSharp.
- Certifique-se de que o conector forneça metadados adequados de etapas de preço; `StartProtection` interpreta distâncias em preços de instrumentos.
- O modelo é intencionalmente minimalista e não gerencia preenchimentos parciais ou entradas de pirâmide além do limite de `MaxOrders`.
