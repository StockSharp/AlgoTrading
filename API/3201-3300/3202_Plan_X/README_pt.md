# Estratégia de Plan X
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Plan X é um sistema de rompimento convertido do consultor especialista original de MetaTrader 5. Avalia o fechamento de cada vela finalizada em relação a uma vela de referência deslocada por um número configurável de barras. Quando o fechamento mais recente excede o fechamento de referência por uma altura de canal especificada, a estratégia abre uma posição na direção do rompimento. A reversão de sinal opcional permite negociar rompimentos na direção oposta.

A implementação usa a API de alto nível do StockSharp. Suporta stop-loss ajustável, take-profit, lógica de trailing stop e um filtro de sessão de trading.

## Como funciona

1. **Processamento de velas** – a estratégia assina o tipo de vela configurado e processa apenas velas finalizadas. Um breve histórico de fechamentos é mantido para comparar o último valor com uma barra de referência deslocada.
2. **Detecção de rompimento** – se o último fechamento for maior que o fechamento de referência por mais que a altura do canal, um sinal comprado é produzido. Se for menor pelo mesmo valor, um sinal vendido é gerado. Quando o indicador de reversão está habilitado, os sinais são invertidos.
3. **Execução de ordens** – a estratégia usa ordens de mercado. Ao reverter de uma posição oposta, o volume da ordem inclui automaticamente o valor absoluto da posição atual para achatar e re-entrar em uma única operação.
4. **Gestão de risco** – os níveis de stop-loss e take-profit são definidos imediatamente após a entrada. Um trailing stop pode substituir o stop original quando o preço se move favoravelmente por mais do que a distância de trailing mais o passo de trailing.
5. **Filtro de tempo** – o trading pode ser limitado a uma hora de início e fim. Se a hora de início for maior que a hora de fim, a janela é tratada como cruzando a meia-noite.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Stop Loss (pips)` | Distância do stop de proteção em pips, convertida para unidades de preço baseadas no passo de preço do instrumento. |
| `Take Profit (pips)` | Distância do alvo em pips. |
| `Trailing Stop (pips)` | Distância entre o preço e o trailing stop. Definir como zero para desabilitar o trailing. |
| `Trailing Step (pips)` | Lucro adicional necessário antes que o trailing stop avance. Deve ser positivo quando o trailing está habilitado. |
| `Channel Height (pips)` | Limiar de rompimento expresso em pips. |
| `Candle Shift` | Número de barras entre o último fechamento e a vela de referência. |
| `Use Time Control` | Habilita ou desabilita o filtro de sessão de trading. |
| `Start Hour` | Primeira hora (0–23) quando o trading é permitido. |
| `End Hour` | Última hora (0–23) quando o trading é permitido. |
| `Reverse Signals` | Inverte a direção do rompimento. |
| `Order Volume` | Tamanho da ordem de mercado expresso em lotes/contratos. |
| `Candle Type` | Tipo de dados de velas utilizado para análise. |

## Lógica de sinais

- **Entrada comprada** – último fechamento ≥ fechamento de referência + altura do canal, reversão desabilitada.
- **Entrada vendida** – último fechamento ≤ fechamento de referência − altura do canal, reversão desabilitada.
- Quando a reversão está habilitada, a lógica troca as condições comprada e vendida.

## Lógica de trailing stop

- O trailing stop é ativado quando o movimento favorável excede `Trailing Stop + Trailing Step` em termos de preço.
- Para posições compradas o stop é movido para `high − Trailing Stop` se o novo valor for maior que o stop existente.
- Para posições vendidas o stop é movido para `low + Trailing Stop` se o novo valor for menor que o stop existente.

## Notas adicionais

- O cálculo do tamanho do pip emula a versão MQL multiplicando o passo de preço por 10 para instrumentos de 3 ou 5 decimais.
- O trading fora da sessão permitida ignora novas entradas, mas continua gerenciando posições abertas.
- A estratégia chama `StartProtection()` uma vez durante a inicialização para habilitar os serviços de proteção de portfólio integrados.
