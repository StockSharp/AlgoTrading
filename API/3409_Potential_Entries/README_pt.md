# Estratégia de entradas potenciais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Entradas Potenciais** replica a lógica do consultor especialista `EA_PotentialEntries.mq5` original. Ele analisa pares das velas finalizadas mais recentes e emite negociações quando aparecem padrões específicos de reversão ou impulso de duas velas. A estratégia funciona em uma direção de cada vez (alta ou baixa), selecionável através do parâmetro `Pattern Side`. Os níveis de stop de proteção são recalculados em cada entrada para espelhar o posicionamento original do stop MetaTrader no extremo do par de velas analisado.

A implementação usa o API de alto nível de StockSharp: ele assina o tipo de vela configurado, processa o fluxo dentro de `ProcessCandle`, abre posições com `BuyMarket`/`SellMarket` e fecha negociações por meio de saídas de mercado quando o preço stop rastreado internamente é violado. Os gráficos renderizam a série de velas junto com as negociações estratégicas para uma rápida inspeção visual.

## Dados e parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| Geral | Lado padrão | Direção da varredura de padrão: `Bullish` procura reversões de alta, `Bearish` procura reversões de baixa. |
| Negociação | Volume comercial | Tamanho da ordem de mercado usado para cada entrada. A estratégia nivela a exposição oposta antes de abrir uma nova posição. |
| Geral | Tipo de vela | Série de velas usadas para reconhecimento de padrões (padrão: velas horárias). |

## Lógica de negociação
A estratégia avalia a vela finalizada mais recente (`C1`) juntamente com a vela anterior (`C2`). Todas as medidas do pavio e do corpo são calculadas em unidades de preço.

### Modo de alta
Quando `Pattern Side = Bullish`, as seguintes configurações acionam uma entrada longa:
1. **Martelo de alta**
   - `C1` fecha acima de sua abertura, enquanto `C2` está em baixa.
   - O pavio inferior de `C1` é pelo menos o dobro do corpo e mais que o triplo do pavio superior.
   - Uma ordem de compra de mercado é enviada e o nível de stop é definido para o menor dos mínimos de `C1` e `C2`.
2. **Martelo Invertido de Alta**
   - `C1` é otimista e `C2` é baixista.
   - O pavio superior de `C1` tem pelo menos o dobro do corpo e pelo menos o triplo do pavio inferior.
   - Executa a mesma ordem e lógica de parada da configuração do martelo.
3. **Construtor de impulso de alta**
   - `C1` e `C2` estão otimistas.
   - O intervalo de `C1` é maior que o intervalo de `C2`, e o corpo de `C1` é pelo menos duas vezes o corpo de `C2`.
   - Abre uma posição longa com stop abaixo do mínimo mínimo do par.

### Modo de baixa
Quando `Pattern Side = Bearish`, as seguintes configurações acionam uma entrada curta:
1. **Estrela cadente**
   - `C1` fecha abaixo de sua abertura enquanto `C2` está otimista.
   - O pavio superior de `C1` tem pelo menos o dobro do corpo e pelo menos o triplo do pavio inferior.
   - Uma ordem de venda a mercado é enviada com o stop colocado acima da máxima mais alta de `C1` e `C2`.
2. **Homem Enforcado**
   - `C1` é de baixa e `C2` é de alta.
   - O pavio inferior de `C1` é pelo menos o dobro do corpo e mais que o triplo do pavio superior.
   - Abre uma posição curta e usa a mesma lógica de stop da estrela cadente.
3. **Construtor de impulso de baixa**
   - `C1` e `C2` estão em baixa.
   - O corpo de `C1` é maior que o corpo de `C2`, e o intervalo de `C1` é pelo menos duas vezes o intervalo de `C2`.
   - Entra vendido e armazena o stop acima da máxima máxima das velas analisadas.

### Gerenciamento de parada e tratamento de posição
- Apenas um modo direcional está ativo por vez. Antes de entrar numa negociação, a estratégia fecha qualquer posição na direção oposta.
- Cada entrada registra um preço stop no extremo do par de velas. Na chegada de cada nova vela finalizada, a estratégia verifica se a mínima (para posições compradas) ou a máxima (para posições vendidas) viola o nível armazenado e fecha a posição com uma ordem de mercado, se acionada.
- Quando nenhuma posição está aberta, o valor de parada armazenado é apagado, garantindo que os níveis obsoletos nunca sejam reutilizados.

## Notas de uso
- Escolha o modo `Bullish` ou `Bearish` dependendo se você deseja procurar oportunidades longas ou curtas.
- As velas horárias padrão podem ser substituídas por qualquer outro tipo de dados de vela disponível.
- Ainda não há porta Python, conforme solicitado. Somente a implementação C# é fornecida.
- A estratégia não estabelece metas de lucro. As saídas dependem exclusivamente da lógica de parada baseada em velas ou de intervenção manual.
