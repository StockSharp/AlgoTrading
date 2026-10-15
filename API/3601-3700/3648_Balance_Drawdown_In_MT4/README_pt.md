# Rebaixamento de saldo na estratégia MT4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transporta o consultor especialista MetaTrader 4 original **BalanceDrawdownInMT4** para o StockSharp API de alto nível. O EA abre imediatamente uma única posição longa e mede continuamente a redução da conta em relação ao saldo máximo alcançado desde o início da sessão.

## Lógica de negociação

1. Quando a estratégia é iniciada, ela chama `StartProtection` para armar níveis gerenciados de stop-loss e take-profit que imitam as entradas MQL expressas em faixas de preço.
2. Na primeira vela finalizada (período padrão: 1 minuto) a estratégia verifica se uma posição está aberta. Se não existir exposição, ele envia uma ordem de compra a mercado usando o `Volume` configurado.
3. Após cada vela finalizada, a métrica de rebaixamento é atualizada:
   - A estratégia rastreia o saldo máximo alcançado como **StartBalance + PnL realizado**.
   - O patrimônio atual é igual a **StartBalance + PnL realizado + PnL não realizado**, onde o PnL não realizado é derivado do último preço de fechamento da vela, do preço médio de entrada e do `PriceStep`/`StepPrice` do instrumento.
   - Drawdown é o declínio percentual do saldo máximo armazenado até o patrimônio atual. O valor é registrado com uma mensagem informativa a cada atualização.

O algoritmo nunca abre posições adicionais ou reverte. Uma vez estabelecida a posição inicial, ela permanece ativa até ser interrompida, o take-profit dispara ou o usuário intervém manualmente.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `StartBalance` | `1000` | Saldo da linha de base usado ao calcular o patrimônio máximo e a porcentagem de redução. |
| `Volume` | `0.01` | Volume líquido (em unidades do instrumento) da ordem inicial de compra no mercado. |
| `StopLossPoints` | `300` | Distância do preço de entrada até o stop de proteção, medida em faixas de preço. Um valor de `0` desativa a parada. |
| `TakeProfitPoints` | `400` | Distância do preço de entrada até a meta de proteção, medida em faixas de preço. Um valor de `0` desativa o destino. |
| `CandleType` | `1m` período de tempo | Período que orienta atualizações periódicas de saque e verificação inicial de entrada. |

## Notas de implementação

- O contador de rebaixamento usa o PnL realizado da estratégia (`PnL`) combinado com o PnL não realizado estimado a partir de diferenças de preço, correspondendo à lógica de saldo corrente encontrada na versão MT4.
- Se `PriceStep` ou `StepPrice` não estiver disponível para o título, o cálculo de PnL não realizado retornará zero com segurança, evitando erros de divisão por zero.
- `Volume` é validado para garantir um valor positivo antes da negociação inicial; caso contrário, um aviso será registrado e a estratégia permanecerá estável.
- `DrawdownPercent` expõe a leitura de redução mais recente para que outros módulos (painel, controladores de risco) possam extrair o valor programaticamente.

## Dicas de uso

- Defina `StartBalance` para o saldo real da conta (ou o saldo no início da sessão de negociação) para obter estatísticas de rebaixamento significativas.
- Mantenha as velas padrão de 1 minuto para atualizações oportunas ou escolha um tipo de vela sintética mais rápida se precisar de precisão próxima ao tick.
- Como esta estratégia mantém intencionalmente uma única posição longa, combine-a com controles de risco manuais ou automação externa se precisar entrar novamente após um stop ou alvo ser atingido.
- Sempre teste em um simulador para confirmar se o corretor fornece `PriceStep` e `StepPrice` para que a conversão de PnL não realizada corresponda às expectativas.
