# Estratégia ColorJFatl Digit NN3 MMRec (Conversão StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port de alto nível do StockSharp do especialista MetaTrader 5 *Exp_ColorJFatl_Digit_NN3_MMRec*. O robô original usava um indicador ColorJFatl_Digit personalizado junto com regras de recuperação de gestão de dinheiro. A versão StockSharp foca no motor de sinal central e o expressa através de três módulos independentes que trabalham em diferentes períodos de tempo.

Cada módulo aplica uma Média Móvel Jurik (JMA) à fonte de preço selecionada e monitora a inclinação dessa média. Quando a inclinação se torna positiva, o módulo trata isso como um regime de alta, fecha a exposição vendida e opcionalmente abre uma nova posição comprada. Quando a inclinação se torna negativa, o módulo executa a lógica espelho para negociações vendidas. Todos os módulos compartilham o mesmo portfólio e portanto sempre trabalham com a posição líquida da estratégia.

## Lógica de negociação

1. Assinar velas em três períodos de tempo (padrões: 1 dia, 8 horas, 3 horas).
2. Para cada vela concluída:
   - Converter a vela para o preço aplicado configurado (fechamento, abertura, preço típico, preço DeMark, etc.).
   - Processar o valor através de uma Média Móvel Jurik para obter uma série suavizada.
   - Comparar o valor JMA atual com o anterior para determinar a direção da inclinação. Uma inclinação positiva produz um estado "para cima", uma inclinação negativa produz um estado "para baixo", uma inclinação plana mantém o estado anterior.
   - Armazenar em buffer os estados de acordo com o atraso *SignalBar* para que a estratégia possa agir em barras históricas se desejado (o especialista original suportava sinais atrasados).
3. Sempre que um módulo detectar uma transição:
   - **Transição para cima**: opcionalmente fechar qualquer posição vendida e abrir uma posição comprada com o volume do módulo.
   - **Transição para baixo**: opcionalmente fechar qualquer posição comprada e abrir uma posição vendida com o volume do módulo.
4. Sinais opostos de outro módulo podem zerar ou reverter a posição dependendo das flags de habilitação.

Stops e lucros não são codificados; em vez disso, a estratégia depende de sinais opostos e das proteções integradas do StockSharp (`StartProtection()`) para segurança.

## Parâmetros

Os parâmetros são agrupados por módulo (A, B, C) para espelhar o template MT5. Cada grupo expõe os seguintes valores:

- **CandleType** – período de tempo para velas de entrada.
- **JmaLength** – período da Média Móvel Jurik.
- **JmaPhase** – armazenado para documentação; o JMA do StockSharp não expõe ajuste de fase.
- **SignalBar** – número de barras concluídas a aguardar antes de agir em um sinal (0 = imediato).
- **AppliedPrices** – transformação de preço usada como entrada para JMA (fechamento, abertura, mediana, típico, ponderado, simples, quarto, seguimento de tendência, DeMark).
- **AllowBuyOpen / AllowSellOpen** – permissão para abrir posições na direção correspondente.
- **AllowBuyClose / AllowSellClose** – permissão para fechar posições existentes quando o módulo emite um sinal oposto.
- **Volume** – tamanho da ordem que o módulo usa ao abrir uma nova negociação.

Como os módulos compartilham um único portfólio de conta, apenas uma posição líquida comprada ou líquida vendida pode existir por vez. Se um módulo tentar abrir uma negociação enquanto o portfólio já carrega exposição na mesma direção, a ordem é ignorada; se uma direção oposta estiver aberta, ela é fechada antes que a nova negociação seja colocada (sujeito às flags de permissão).

## Notas de uso

- A estratégia assina todos os períodos de tempo configurados através de `GetWorkingSecurities()`, garantindo que o ambiente de simulação ou ao vivo carregue as séries de velas necessárias.
- Os sinais operam estritamente em velas concluídas para evitar o redesenho intra-barra.
- O enum *AppliedPrices* reproduz as opções do indicador original, incluindo duas variantes de preço de seguimento de tendência e o preço DeMark.
- A lógica de recuperação de gestão de dinheiro da versão MQL não está reproduzida. Em vez disso, o risco pode ser gerenciado pelas proteções do StockSharp ou ajustando os volumes dos módulos.
- Comentários em inglês dentro do código explicam cada etapa da conversão para manutenção mais fácil e port futuro para Python.

## Estendendo a estratégia

- Para adicionar regras de stop-loss ou take-profit, substituir a chamada padrão `StartProtection()` pela configuração desejada.
- Módulos adicionais podem ser criados clonando o padrão de configuração `SignalModule`.
- Para gestão avançada de posições (por exemplo, rastrear exposição por módulo), estratégias filhas ou portfólios virtuais do StockSharp podem ser adicionados sobre esta base.
