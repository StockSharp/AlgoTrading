# Estratégia de Pendulum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema martingale baseado em grade que oscila entre dois limiares de preço. A estratégia abre uma posição comprada quando o preço atinge o limite superior da grade e vira para uma posição vendida com volume aumentado quando o preço se move para o limite inferior. Continua alternando direções (até um número configurável de camadas) enquanto expande os alvos e reduz as distâncias de proteção de acordo com o consultor especializado original Pendulum. Após realizar lucro, o motor reinicia a grade e agenda uma nova entrada no mesmo nível para manter o movimento pendular.

## Detalhes

- **Lógica de entrada**
  - Alinha a grade ao preço de fechamento do candle usando o `StepSize` configurado.
  - **Gatilho do limite superior acionado** → abre uma posição comprada com o volume base.
  - **Gatilho do limite inferior acionado** → abre uma posição vendida com o volume base.
  - Quando a posição ativa se move para o gatilho oposto, a estratégia reverte a direção, multiplica o volume absoluto por `Multiplier` e atualiza as distâncias de take-profit/stop-loss como a versão MQL.
  - As reentradas são agendadas após saídas lucrativas para que o próximo candle possa reabrir imediatamente no mesmo nível de grade assim que as ordens de fechamento forem processadas.
- **Lógica de saída**
  - Cada camada define um take-profit dedicado: um passo para a primeira camada, `Multiplier` passos para cada camada subsequente.
  - Os stops de proteção espelham a lógica MQL: a primeira camada usa um stop amplo (`StepSize * Multiplier`), camadas subsequentes usam um stop de um passo contra a nova direção.
  - Quando o número máximo de camadas é atingido, a estratégia aguarda take-profit ou stop-loss antes de reiniciar.
- **Gestão de posição**
  - Usa netting: o port do StockSharp fecha e reverte a posição agregada em vez de manter comprados e vendidos cobertos. Isso preserva a exposição do consultor original enquanto permanece compatível com os portfólios do StockSharp.
  - O volume é arredondado para o passo de volume do instrumento quando disponível.
- **Dados**
  - Funciona com qualquer símbolo e período. A assinatura padrão usa candles de 1 minuto e depende dos preços de fechamento dos candles para as verificações da grade.
- **Proteção integrada**
  - `StartProtection()` está habilitado para proteger posições inesperadas deixadas após desconexões ou intervenção manual.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `StepSize` | `0.001` | Distância entre níveis de grade. A grade sempre se encaixa em múltiplos deste valor. |
| `Multiplier` | `2` | Multiplica tanto o volume do trade quanto os alvos estendidos quando a direção vira para uma nova camada. Deve ser maior que 1. |
| `MaxLayers` | `3` | Número máximo de camadas martingale antes de a estratégia parar de adicionar novas reversões. |
| `BaseVolume` | `1` | Tamanho base do trade usado para a primeira camada. Camadas posteriores escalam por `Multiplier`. |
| `CandleType` | `1 Minute TimeFrame` | Tipo de candle usado para assinatura. Pode ser alterado para qualquer outro período suportado pela fonte de dados. |

## Notas

- A estratégia recria o comportamento de `Pendulum.mq5` sem depender de posições cobertas. Como o StockSharp consolida a exposição, a posição líquida é revertida para emular as grades MQL.
- As conclusões de take-profit acionam uma ordem diferida para que o próximo candle possa reabrir imediatamente no mesmo nível de preço assim que o trade de fechamento for processado.
- Manter o tamanho de passo configurado alinhado com o passo de preço do instrumento para evitar arredondamentos excessivos dos níveis da grade.
