# Estratégia de Exp Highs Lows Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Exp Highs Lows Signal é um port direto do assessor especialista MetaTrader 5 `Exp_HighsLowsSignal`. A estratégia depende de um detector de padrões que busca um número configurável de velas consecutivas que imprimem máximas mais altas e mínimas mais altas (sequência altista) ou máximas mais baixas e mínimas mais baixas (sequência baixista). Uma vez encontrada uma sequência, a estratégia atrasa a reação pelo número configurado de barras fechadas, fecha qualquer exposição oposta e opcionalmente abre uma posição na direção detectada. Os stops protetores são expressos em steps de preço para refletir o gerenciamento de dinheiro baseado em pontos do robô original.

## Lógica da estratégia
### Detector de sequência de Highs/Lows
* O detector avalia cada vela finalizada no período selecionado.
* Um **sinal altista** requer `SequenceLength` comparações consecutivas onde tanto a máxima atual quanto a mínima atual são estritamente maiores que a barra anterior.
* Um **sinal baixista** requer `SequenceLength` comparações consecutivas onde tanto a máxima atual quanto a mínima atual são estritamente menores que a barra anterior.
* Os sinais são enfileirados e liberados após `SignalBarDelay` velas fechadas, correspondendo à configuração `SignalBar` da implementação MQL.

### Regras de entrada
* **Entradas longas**
  * Acionadas quando uma sequência altista se torna ativa e `AllowLongEntry` está habilitado.
  * Qualquer posição curta existente é fechada primeiro (se `AllowShortExit` for verdadeiro), então uma ordem de compra de mercado com volume `OrderVolume + |Position|` é enviada para cobrir shorts e estabelecer o tamanho longo desejado.
* **Entradas curtas**
  * Acionadas quando uma sequência baixista se torna ativa e `AllowShortEntry` está habilitado.
  * Qualquer posição longa existente é fechada primeiro (se `AllowLongExit` for verdadeiro), então uma ordem de venda de mercado com volume `OrderVolume + |Position|` é enviada para cobrir longos e estabelecer o tamanho curto desejado.

### Regras de saída
* Uma sequência altista sempre solicita `AllowShortExit` para fechar shorts abertos.
* Uma sequência baixista sempre solicita `AllowLongExit` para fechar longos abertos.
* Quando a flag relevante está desabilitada, a exposição oposta permanece intacta, permitindo ao usuário negociar apenas em uma direção ou executar o detector em modo "somente alertas".

### Gestão de risco
* `StopLossTicks` e `TakeProfitTicks` representam distâncias em steps de preço (pontos). Um valor de `0` desabilita a ordem protetora correspondente, reproduzindo o comportamento do EA original.
* `StartProtection` converte essas distâncias em offsets de preço absolutos para que todas as entradas de mercado recebam automaticamente ordens de stop-loss e take-profit correspondentes.

## Parâmetros
* **OrderVolume** – volume base da ordem usado quando uma nova negociação é aberta.
* **AllowLongEntry / AllowShortEntry** – interruptores que habilitam entradas longas ou curtas em seus respectivos sinais.
* **AllowLongExit / AllowShortExit** – interruptores que permitem à estratégia achatar posições opostas quando o sinal contra-tendência aparece.
* **StopLossTicks / TakeProfitTicks** – distâncias protetoras em steps de preço; defina como `0` para desabilitar.
* **SequenceLength** – número de comparações consecutivas necessárias para qualificar uma sequência altista ou baixista (equivalente a `HowManyCandles` no MT5).
* **SignalBarDelay** – número de velas fechadas a aguardar antes de agir em um sinal (equivalente ao input `SignalBar`).
* **CandleType** – período usado para construir o detector de Highs/Lows (padrão: velas de 4 horas).

## Notas adicionais
* A estratégia armazena apenas a quantidade mínima de histórico de velas necessária para o detector, mantendo o comportamento idêntico ao indicador personalizado do MT5.
* Como todo o gerenciamento de ordens ocorre através de `StartProtection`, backtests e trading ao vivo recebem automaticamente ordens de stop e take-profit correspondentes sem código adicional.
* Desabilite as flags `Allow` correspondentes para converter a estratégia em um filtro direcional ou uma ferramenta de sinalização pura.
* Nenhuma tradução Python é fornecida; apenas a versão C# está disponível neste pacote.
