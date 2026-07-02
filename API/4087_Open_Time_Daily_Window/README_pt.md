# Estratégia de janela diária de tempo aberto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reproduz o comportamento do especialista MetaTrader "OpenTime". Ele coloca ordens de mercado em um horário do dia configurável, opcionalmente fecha todas as exposições durante uma janela de saída dedicada e aplica regras simples de gerenciamento de dinheiro, como stop-loss fixo, take-profit e proteção móvel. A porta usa o StockSharp `Strategy` API de alto nível, para que a estratégia possa ser combinada com outros componentes dentro da estrutura.

## Como funciona
1. Cada vela concluída no período selecionado aciona uma verificação da hora do dia.
2. Quando a hora atual cai dentro da janela de negociação, a estratégia envia ordens de mercado para todas as direções habilitadas:
   * Se apenas um lado estiver habilitado, a posição líquida atual será estendida ou invertida até que o volume solicitado seja alcançado.
   * Quando ambos os lados estão habilitados, as ordens de compra e venda são emitidas na mesma janela. Como StockSharp contabiliza a exposição líquida lado a lado, a abertura da segunda direção compensa automaticamente a exposição oposta antes de estabelecer a nova.
3. Enquanto a janela de fechamento estiver ativa, a estratégia chama `ClosePosition()` uma vez para nivelar qualquer exposição pendente.
4. As distâncias opcionais de stop-loss, take-profit e trailing stop são delegadas a `StartProtection`, que gerencia as ordens de proteção usando saídas de mercado.

## Parâmetros
- **Ativar Fechar Janela** – espelha o sinalizador `TimeClose`. Quando ativados, `Close Position Time` e `Window Length` definem quando as negociações existentes são fechadas.
- **Horário de posição fechada** – horário diário de início da janela de saída (padrão 20:50).
- **Horário de negociação** – horário diário em que novas negociações são permitidas (padrão 18h50).
- **Duração da janela** – duração das janelas de negociação e fechamento (padrão 5 minutos, correspondendo à entrada original `Duration`).
- **Permitir entradas de venda** – corresponde à opção MQL `Sell`; habilita entradas curtas (padrão verdadeiro).
- **Permitir entradas de compra** – corresponde à opção MQL `Buy`; permite entradas longas (padrão falso).
- **Volume do pedido** – volume líquido alvo para cada nova negociação (padrão 0,1 lote). A estratégia soma o valor absoluto da posição atual quando aparece um sinal oposto, de forma que as reversões ocorram em uma única ordem de mercado.
- **Stop-Loss Points** – distância em pontos para a parada de proteção (padrão 0 desabilita a parada).
- **Pontos Take-Profit** – distância em pontos para a meta de lucro (o padrão 0 desativa a meta).
- **Use Trailing Stop** – ativa a lógica de trailing stop do auxiliar `SimpleTrailing` original.
- **Pontos de parada final** – distância final expressa em pontos (padrão 300).
- **Trailing Step Points** – progresso adicional necessário antes de avançar o trailing stop (padrão 3).
- **Tipo de vela** – período de tempo usado para as verificações de tempo (velas padrão de 1 minuto).

## Notas
- O tamanho do ponto é derivado da etapa do preço do título. Para aspas de três e cinco decimais, a etapa é multiplicada por 10, reproduzindo o tratamento de pip usado pelo script MQL.
- `StartProtection` anexa paradas de proteção somente quando pelo menos uma das distâncias é maior que zero. Se o rastreamento estiver ativo sem um stop loss fixo, a distância de rastreamento será fornecida como o valor de proteção inicial.
- A estratégia intencionalmente não gerencia ordens pendentes ou tentativas repetidas, porque StockSharp já fornece tratamento automático de erros para ordens de mercado.
