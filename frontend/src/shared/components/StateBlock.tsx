interface StateBlockProps {
  message: string;
}

export function StateBlock({ message }: StateBlockProps) {
  return <div className="state-block">{message}</div>;
}
