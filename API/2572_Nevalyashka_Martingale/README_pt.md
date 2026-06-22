# Estratégia Nevalyashka Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Nevalyashka Martingale é uma portagem direta do consultor especialista do MetaTrader 5 "Nevalyashka3_1". Executa um martingale de símbolo único que alterna entre comprar e vender após operações com perda. A estratégia sempre começa vendendo e mede o capital da conta para decidir se o ciclo de operações anterior terminou em lucro ou perda. Um lucro reinicia o volume para o tamanho de lote base e mantém a direção inalterada, enquanto uma perda multiplica o tamanho do lote e inverte a direção numa tentativa de recuperar o drawdown.

## Como funciona
- **Operação inicial** – uma posição curta é aberta na primeira vela concluída usando o tamanho de lote base.
- **Acompanhamento de capital** – a estratégia armazena o valor de capital mais alto observado. Quando não há posição aberta, compara o capital atual com o pico armazenado.
  - Se o capital marcou um novo máximo, a próxima operação usa o tamanho de lote base e repete a última direção.
  - Se o capital não marcou um novo máximo, a próxima operação aumenta o lote pelo multiplicador e muda de direção.
- **Stop loss / take profit** – cada ordem usa distâncias fixas definidas em "pontos" (passos do instrumento). O take profit espelha o especialista original: o stop fica `StopLossPoints` afastado da entrada e o alvo está `TakeProfitPoints` afastado.
- **Trailing** – uma vez que o preço se move por `MoveProfitPoints`, o stop é ajustado. Cada movimento requer um buffer adicional de `MoveStepPoints` para que o stop só avance quando o mercado continua empurrando. Quando o stop vai além do preço de entrada, o volume planejado é dividido pelo multiplicador, reduzindo a próxima operação em direção ao lote base.
- **Saída de posição** – a posição fecha imediatamente quando a máxima/mínima da vela atinge os níveis de stop ou take. Após o fechamento, a estratégia avalia o capital e prepara o próximo sinal.

## Parâmetros
- `BaseVolume` – tamanho de lote para a operação inicial e qualquer ciclo lucrativo (padrão 0.1).
- `VolumeMultiplier` – fator aplicado após uma perda para aumentar o próximo lote (padrão 1.1).
- `TakeProfitPoints` – distância de take-profit medida em pontos de preço (padrão 94).
- `MoveProfitPoints` – excursão favorável mínima antes que o stop trailing seja ativado (padrão 25).
- `MoveStepPoints` – buffer extra necessário entre ajustes de trailing sucessivos (padrão 11).
- `StopLossPoints` – distância inicial do stop-loss medida em pontos de preço (padrão 70).
- `CandleType` – período usado para gestão de operações. O padrão são velas de 5 minutos.

## Detalhes de gestão de posição
- A estratégia mantém `_plannedVolume` para espelhar a variável original "Lot". Só muda após fechar uma operação ou quando o stop ultrapassa o break-even.
- `AdjustVolume` respeita as regras de exchange alinhando o tamanho do lote ao `VolumeStep` e impondo `MinVolume`/`MaxVolume`.
- `GetPointValue` replica a lógica de "ponto ajustado" do MT5: para instrumentos cotados com 3 ou 5 decimais, o tamanho do ponto é multiplicado por 10 para trabalhar com pips inteiros.
- `HandleLongPosition` e `HandleShortPosition` usam máximas e mínimas de velas para emular a modificação de stops e o comportamento de saída do MT5 sem depender do histórico de indicadores.

## Notas de uso
- A estratégia assume que opera com um único ativo. Adicione-a à estratégia e configure `Security`/`Portfolio` antes de iniciar.
- Como é um martingale, o risco cresce rapidamente após uma série de perdas. Ajuste `BaseVolume` e `VolumeMultiplier` com cuidado e teste com requisitos de margem realistas.
- As distâncias de stop e take-profit são definidas em pontos do instrumento. Certifique-se de que os metadados do ativo (`PriceStep`, `VolumeStep`, `MinVolume`) estejam preenchidos para que os deslocamentos e cálculos de lote correspondam ao seu broker.
- A lógica de trailing age sobre velas terminadas. Golpes de stop intrabar podem ocorrer mais cedo em operativa real dependendo do caminho do preço.
