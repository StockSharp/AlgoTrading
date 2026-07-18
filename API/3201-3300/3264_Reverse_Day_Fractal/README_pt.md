# Estratégia de Reverse Day Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Reverse Day Fractal é uma estratégia de price action que busca reversões bruscas após um rompimento intradiário. O algoritmo analisa as últimas três velas concluídas. Quando a barra atual forma um novo extremo além das duas velas anteriores e fecha de volta na direção oposta, ela trata isso como um rompimento falho e entra em um trade de reversão. Ordens protetoras são gerenciadas através de distâncias configuráveis de take-profit, stop-loss e trailing stop medidas em passos de preço.

## Lógica de trading
- **Configuração altista**:
  - A vela concluída atual faz uma *mínima mais baixa* do que cada uma das duas velas anteriores.
  - A vela fecha *acima* do seu preço de abertura, indicando uma rejeição altista da nova mínima.
  - Quando essas condições são atendidas e a estratégia pode operar, ela abre uma posição comprada. Opcionalmente pode fechar um vendido existente primeiro.
- **Configuração baixista**:
  - A vela concluída atual faz uma *máxima mais alta* do que cada uma das duas velas anteriores.
  - A vela fecha *abaixo* do seu preço de abertura, indicando uma rejeição baixista da nova máxima.
  - Quando essas condições são satisfeitas, ela abre uma posição vendida, opcionalmente fechando um comprado existente primeiro.
- **Gerenciamento de posição**: a estratégia pode ser configurada para permitir apenas uma posição aberta de cada vez (comportamento padrão). Quando desabilitado, reverterá uma posição existente adicionando o volume necessário para mudar a direção.
- **Controles de risco**: ao iniciar, a estratégia chama `StartProtection` para aplicar proteções de take-profit, stop-loss e trailing stop usando as distâncias de ponto configuradas. Quando um trailing stop está habilitado, o stop protetor seguirá o preço em passos discretos.

## Parâmetros
- `Trade Volume` – volume de ordem para novas entradas.
- `Take Profit` – distância ao alvo de lucro medida em passos de preço. Zero para desabilitar.
- `Stop Loss` – distância ao stop protetor medida em passos de preço. Zero para desabilitar.
- `Trailing Stop` – distância de trailing stop em passos de preço. Zero para desabilitar.
- `Trailing Step` – movimento mínimo (em passos) necessário antes de ajustar o trailing stop.
- `Only One Position` – quando habilitado, a estratégia ignora novos sinais enquanto uma posição está aberta.
- `Candle Type` – tipo de dados de velas usado para os cálculos (padrão: período de 1 hora).

## Notas
- Os sinais são gerados apenas em velas concluídas fornecidas pela assinatura configurada.
- A estratégia mantém os dois extremos de vela mais recentes na memória; portanto, precisa de pelo menos duas velas concluídas após o início antes de poder gerar um sinal.
- Os valores de parâmetros padrão replicam o consultor especialista MQL4 original: volume de 0,01 lote, stop loss de 20 pontos, take profit de 10 pontos, trailing stop de 25 pontos e trailing step de 5 pontos.
